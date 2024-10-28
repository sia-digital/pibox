using Hangfire;
using Hangfire.Dashboard;
using Hangfire.MemoryStorage;
using Hangfire.PostgreSql;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Newtonsoft.Json;
using PiBox.Hosting.Abstractions;
using PiBox.Hosting.Abstractions.Extensions;
using PiBox.Hosting.Abstractions.Plugins;
using PiBox.Hosting.Abstractions.Services;
using PiBox.Plugins.Jobs.Hangfire.Attributes;
using PiBox.Plugins.Jobs.Hangfire.Job;
using BindingFlags = System.Reflection.BindingFlags;

namespace PiBox.Plugins.Jobs.Hangfire
{
    public class HangFirePlugin : IPluginServiceConfiguration, IPluginApplicationConfiguration,
        IPluginHealthChecksConfiguration
    {
        private readonly IImplementationResolver _implementationResolver;
        private readonly IHangfireConfigurator[] _configurators;
        private readonly HangfireConfiguration _hangfireConfig;

        public HangFirePlugin(HangfireConfiguration configuration, IImplementationResolver implementationResolver, IHangfireConfigurator[] configurators)
        {
            _implementationResolver = implementationResolver;
            _configurators = configurators;
            _hangfireConfig = configuration;
        }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddFeatureManagement();
            serviceCollection.AddHangfire(conf =>
                {
                    conf.UseSerializerSettings(new JsonSerializerSettings());
                    if (_hangfireConfig.InMemory)
                        conf.UseMemoryStorage();
                    else
                        conf.UsePostgreSqlStorage(opts => opts.UseNpgsqlConnection(_hangfireConfig.ConnectionString));
                    conf.UseSimpleAssemblyNameTypeSerializer();
                    _configurators.ForEach(x => x.Configure(conf));
                }
            );
            serviceCollection.AddHangfireServer(options =>
            {
                options.Queues = _hangfireConfig.Queues.Union([EnqueuedState.DefaultQueue]).Distinct().ToArray();
                if (_hangfireConfig.PollingIntervalInMs.HasValue)
                    options.SchedulePollingInterval =
                        TimeSpan.FromMilliseconds(_hangfireConfig.PollingIntervalInMs.Value);

                if (_hangfireConfig.WorkerCount.HasValue)
                    options.WorkerCount = _hangfireConfig.WorkerCount.Value;
                _configurators.ForEach(x => x.ConfigureServer(options));
            });
            serviceCollection.AddSingleton<IJobManager>(sp =>
            {
                var jobStorage = sp.GetRequiredService<JobStorage>();
                var recurringJobManager = sp.GetRequiredService<IRecurringJobManager>();
                var backgroundJobClient = sp.GetRequiredService<IBackgroundJobClient>();
                var hasQueueSupport = jobStorage.HasFeature(JobStorageFeatures.JobQueueProperty);
                return new JobManager(hasQueueSupport, jobStorage.GetConnection(), jobStorage.GetMonitoringApi(),
                    recurringJobManager, backgroundJobClient);
            });
            serviceCollection.AddHostedService<HangfireStatisticsMetricsReporter>();
        }

        public void ConfigureApplication(IApplicationBuilder applicationBuilder)
        {
            GlobalJobFilters.Filters.Add(
                new LogJobExecutionFilter(applicationBuilder.ApplicationServices.GetRequiredService<ILoggerFactory>()));
            if (_hangfireConfig.EnableJobsByFeatureManagementConfig)
            {
                GlobalJobFilters.Filters.Add(new EnabledByFeatureFilter(
                    applicationBuilder.ApplicationServices.GetRequiredService<IFeatureManager>(),
                    applicationBuilder.ApplicationServices.GetRequiredService<ILogger<EnabledByFeatureFilter>>()));
            }

            var urlAuthFilter = new HostAuthorizationFilter(_hangfireConfig.AllowedDashboardHost);
            applicationBuilder.UseHangfireDashboard(options: new()
            {
                Authorization = new List<IDashboardAuthorizationFilter> { urlAuthFilter }
            });
            var jobRegister = applicationBuilder.ApplicationServices.GetRequiredService<IJobManager>();
            var jobOptions = applicationBuilder.ApplicationServices.GetService<JobOptions>();
            jobOptions?.ConfigureJobs.Invoke(jobRegister, applicationBuilder.ApplicationServices);
            var registerJobMethod = jobRegister.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Single(x => x.Name == nameof(IJobManager.RegisterRecurring) && x.GetGenericArguments().Length == 1);
            foreach (var job in _implementationResolver.FindTypes(f =>
                         f.HasAttribute<RecurringJobAttribute>() && f.Implements<IAsyncJob>()))
            {
                var recurringJobDetails = job.GetAttribute<RecurringJobAttribute>()!;
                var genericMethod = registerJobMethod.MakeGenericMethod(job);
                genericMethod.Invoke(jobRegister, [recurringJobDetails.CronPattern, recurringJobDetails.Queue]);
            }
        }

        public void ConfigureHealthChecks(IHealthChecksBuilder healthChecksBuilder)
        {
            healthChecksBuilder.AddHangfire(s => s.MinimumAvailableServers = 1, "hangfire",
                tags: [HealthCheckTag.Readiness.Value]);
        }

        internal class HostAuthorizationFilter : IDashboardAuthorizationFilter
        {
            private readonly string _allowedHost;

            public HostAuthorizationFilter(string allowedHost)
            {
                _allowedHost = (allowedHost ?? "").ToLowerInvariant();
            }

            public bool Authorize(DashboardContext context)
            {
                if (string.IsNullOrWhiteSpace(_allowedHost)) return false;
                var incomingRequest = context.GetHttpContext().Request;
                var incomingHost = incomingRequest.Host.Host.ToLowerInvariant();
                return incomingHost == _allowedHost;
            }
        }
    }
}

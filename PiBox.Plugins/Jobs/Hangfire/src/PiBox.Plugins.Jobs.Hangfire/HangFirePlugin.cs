using Hangfire;
using Hangfire.Dashboard;
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
    public class HangFirePlugin(HangfireConfiguration configuration, IImplementationResolver implementationResolver, IHangfireConfigurator[] configurators)
        : IPluginServiceConfiguration, IPluginApplicationConfiguration, IPluginHealthChecksConfiguration
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddFeatureManagement();
            serviceCollection.AddHangfire(conf =>
                {
                    conf.UseSerializerSettings(new JsonSerializerSettings());
                    conf.UseSimpleAssemblyNameTypeSerializer();
                    configurators.ForEach(x => x.Configure(conf));
                }
            );
            serviceCollection.AddHangfireServer(options =>
            {
                options.Queues = configuration.Queues.Union([EnqueuedState.DefaultQueue]).Distinct().ToArray();
                if (configuration.PollingIntervalInMs.HasValue)
                    options.SchedulePollingInterval =
                        TimeSpan.FromMilliseconds(configuration.PollingIntervalInMs.Value);

                if (configuration.WorkerCount.HasValue)
                    options.WorkerCount = configuration.WorkerCount.Value;
                configurators.ForEach(x => x.ConfigureServer(options));
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
            if (configuration.EnableJobsByFeatureManagementConfig)
            {
                GlobalJobFilters.Filters.Add(new EnabledByFeatureFilter(
                    applicationBuilder.ApplicationServices.GetRequiredService<IFeatureManager>(),
                    applicationBuilder.ApplicationServices.GetRequiredService<ILogger<EnabledByFeatureFilter>>()));
            }

            var urlAuthFilter = new HostAuthorizationFilter(configuration.AllowedDashboardHost);
            applicationBuilder.UseHangfireDashboard(options: new()
            {
                Authorization = new List<IDashboardAuthorizationFilter> { urlAuthFilter }
            });
            var jobRegister = applicationBuilder.ApplicationServices.GetRequiredService<IJobManager>();
            var jobOptions = applicationBuilder.ApplicationServices.GetService<JobOptions>();
            jobOptions?.ConfigureJobs.Invoke(jobRegister, applicationBuilder.ApplicationServices);
            var registerJobMethod = jobRegister.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Single(x => x.Name == nameof(IJobManager.RegisterRecurring) && x.GetGenericArguments().Length == 1);
            foreach (var job in implementationResolver.FindTypes(f =>
                         f.HasAttribute<RecurringJobAttribute>() && f.Implements<IAsyncJob>()))
            {
                var recurringJobDetails = job.GetAttribute<RecurringJobAttribute>()!;
                var genericMethod = registerJobMethod.MakeGenericMethod(job);
                genericMethod.Invoke(jobRegister, [recurringJobDetails.CronPattern, recurringJobDetails.Queue, ""]);
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

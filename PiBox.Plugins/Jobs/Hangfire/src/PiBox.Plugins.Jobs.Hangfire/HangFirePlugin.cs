using Hangfire;
using Hangfire.Dashboard;
using Hangfire.MemoryStorage;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using PiBox.Hosting.Abstractions;
using PiBox.Hosting.Abstractions.Extensions;
using PiBox.Hosting.Abstractions.Plugins;
using PiBox.Hosting.Abstractions.Services;
using PiBox.Plugins.Jobs.Hangfire.Job;

namespace PiBox.Plugins.Jobs.Hangfire
{
    public class HangFirePlugin : IPluginServiceConfiguration, IPluginApplicationConfiguration, IPluginHealthChecksConfiguration
    {
        private readonly IImplementationResolver _implementationResolver;
        private readonly HangfireConfiguration _hangfireConfig;

        public HangFirePlugin(HangfireConfiguration configuration, IImplementationResolver implementationResolver)
        {
            _implementationResolver = implementationResolver;
            _hangfireConfig = configuration;
        }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<JobDetailCollection>();
            serviceCollection.AddSingleton<IJobRegister>(sp => sp.GetRequiredService<JobDetailCollection>());
            serviceCollection.AddHangfire(conf =>
                {
                    conf.UseSerializerSettings(new JsonSerializerSettings());
                    if (_hangfireConfig.InMemory)
                    {
                        conf.UseMemoryStorage();
                    }
                    else
                    {
                        conf.UsePostgreSqlStorage(opts => opts.UseNpgsqlConnection(_hangfireConfig.ConnectionString));
                    }

                    conf.UseSimpleAssemblyNameTypeSerializer();
                }
            );
            serviceCollection.AddHangfireServer(options =>
            {
                if (_hangfireConfig.PollingIntervalInMs.HasValue)
                    options.SchedulePollingInterval = TimeSpan.FromMilliseconds(_hangfireConfig.PollingIntervalInMs.Value);

                if (_hangfireConfig.WorkerCount.HasValue)
                    options.WorkerCount = _hangfireConfig.WorkerCount.Value;
            });
            serviceCollection.AddHostedService<HangfireStatisticsMetricsReporter>();
        }

        public void ConfigureApplication(IApplicationBuilder applicationBuilder)
        {
            var urlAuthFilter = new HostAuthorizationFilter(_hangfireConfig.AllowedDashboardHost);
            applicationBuilder.UseHangfireDashboard(options: new() { Authorization = new List<IDashboardAuthorizationFilter> { urlAuthFilter } });
            var jobRegister = applicationBuilder.ApplicationServices.GetRequiredService<IJobRegister>();
            var jobOptions = applicationBuilder.ApplicationServices.GetService<JobOptions>();
            jobOptions?.ConfigureJobs.Invoke(jobRegister, applicationBuilder.ApplicationServices);
            var registerJobMethod = jobRegister.GetType().GetMethod(nameof(IJobRegister.RegisterRecurringAsyncJob))!;
            foreach (var job in _implementationResolver.FindTypes(f => f.HasAttribute<RecurringJobAttribute>() && f.Implements<IAsyncJob>()))
            {
                var recurringJobDetails = job.GetAttribute<RecurringJobAttribute>()!;
                var genericMethod = registerJobMethod.MakeGenericMethod(job);
                genericMethod.Invoke(jobRegister, new object[] { recurringJobDetails.CronPattern });
            }

            jobRegister.ActivateJobs();
        }

        public void ConfigureHealthChecks(IHealthChecksBuilder healthChecksBuilder)
        {
            healthChecksBuilder.AddHangfire(s => s.MinimumAvailableServers = 1, "hangfire",
                tags: new[] { HealthCheckTag.Readiness.Value });
        }

        private class HostAuthorizationFilter : IDashboardAuthorizationFilter
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

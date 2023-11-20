using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using PiBox.Hosting.Abstractions;
using PiBox.Hosting.Abstractions.Metrics;
using PiBox.Hosting.Abstractions.Plugins;
using Unleash;
using Unleash.Events;
using Unleash.Internal;

namespace PiBox.Plugins.Management.Unleash
{
    public class UnleashPlugin : IPluginServiceConfiguration, IPluginHealthChecksConfiguration
    {
        internal const string PiboxUnleashPluginImpressionsTotal = "pibox_unleash_plugin_impressions_total";
        internal const string PiboxUnleashPluginErrorsTotal = "pibox_unleash_plugin_errors_total";
        internal const string PiboxUnleashPluginToggleupdatesTotal = "pibox_unleash_plugin_toggleUpdates_total";
        private readonly UnleashConfiguration _unleashConfiguration;
        private readonly ILogger<UnleashPlugin> _logger;

        private readonly Counter<long> _impressionCounter =
            Metrics.CreateCounter<long>(PiboxUnleashPluginImpressionsTotal, "calls", "Total number of impression events");

        private readonly Counter<long> _errorCounter =
            Metrics.CreateCounter<long>(PiboxUnleashPluginErrorsTotal, "calls", "Total number of error events");

        private readonly Counter<long> _toggleUpdateCounter =
            Metrics.CreateCounter<long>(PiboxUnleashPluginToggleupdatesTotal, "calls", "Total number of toggle update events");

        public UnleashPlugin(UnleashConfiguration unleashConfiguration, ILogger<UnleashPlugin> logger)
        {
            _unleashConfiguration = unleashConfiguration;
            _logger = logger;
        }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            var settings = new UnleashSettings()
            {
                AppName = _unleashConfiguration.AppName,
                UnleashApi = new Uri(_unleashConfiguration.ApiUri),
                InstanceTag = _unleashConfiguration.InstanceTag,
                Environment = _unleashConfiguration.Environment,
                JsonSerializer = new SystemTextSerializer(),
                ProjectId = _unleashConfiguration.ProjectId,
                CustomHttpHeaders =
                    new Dictionary<string, string>() { { "Authorization", _unleashConfiguration.ApiToken } }
            };

            var unleash = new DefaultUnleash(settings);

            // Set up handling of impression and error events
            unleash.ConfigureEvents(cfg =>
            {
                cfg.ImpressionEvent = evt => { HandleImpressionEvent(evt); };
                cfg.ErrorEvent = evt => { HandleErrorEvent(evt); };
                cfg.TogglesUpdatedEvent = evt => { HandleTogglesUpdatedEvent(evt); };
            });

            serviceCollection.AddSingleton<IUnleash>(c => unleash);

            serviceCollection.AddSingleton<IFeatureDefinitionProvider, UnleashFeatureDefinitionProvider>()
                .AddFeatureManagement()
                .AddFeatureFilter<UnleashFilter>()
                .UseDisabledFeaturesHandler(new FeatureNotEnabledDisabledHandler());
            ;
        }

        internal void HandleTogglesUpdatedEvent(TogglesUpdatedEvent evt)
        {
            _toggleUpdateCounter.Add(1);
            _logger.LogInformation("Feature toggles updated on: {evt.UpdatedOn}", evt.UpdatedOn);
        }

        internal void HandleErrorEvent(ErrorEvent evt)
        {
            if (evt.Error != null)
            {
                _errorCounter.Add(1);
                _logger.LogError("Unleash {UnleashError}  of type {UnleashErrorType} occured.", evt.Error, evt.ErrorType);
            }
            else
            {
                // cant find reason why this is happening and no proper error is returned
                // also seems not to degrade or interrupt the service in any kind of way
                _logger.LogDebug("ignore or find out why this is null: {UnleashErrorType} {UnleashError}.",
                    evt.ErrorType, evt.Error);
            }
        }

        internal void HandleImpressionEvent(ImpressionEvent evt)
        {
            _impressionCounter.Add(1, new KeyValuePair<string, object>("featureName", evt.FeatureName),
                new KeyValuePair<string, object>("enabled", evt.Enabled));
            _logger.LogDebug("ImpressionEvent: {UnleashFeatureName}: {UnleashEnabled}", evt.FeatureName,
                evt.Enabled);
        }

        public void ConfigureHealthChecks(IHealthChecksBuilder healthChecksBuilder)
        {
            var url = $"https://{_unleashConfiguration.ApiUri}/health";
            healthChecksBuilder.AddUrlGroup(new Uri(url), "unleash", HealthStatus.Degraded,
                tags: new[] { HealthCheckTag.Readiness.Value });
        }
    }
}

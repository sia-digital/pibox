using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using PiBox.Hosting.Abstractions.Metrics;
using Unleash;

namespace PiBox.Plugins.Management.Unleash
{
    [FilterAlias(FilterAlias)]
    public class UnleashFilter : IFeatureFilter
    {
        private readonly ILogger<UnleashFilter> _logger;
        private readonly IUnleash _unleash;
        private readonly IHttpContextAccessor _httpContextAccessor;
        internal const string FilterAlias = "Unleash";
        internal const string PiboxUnleashPluginUnleashFilterCallsTotal = "pibox_unleash_plugin_unleash_filter_calls_total";

        private readonly Counter<long> _filterCounter =
            Metrics.CreateCounter<long>(PiboxUnleashPluginUnleashFilterCallsTotal, "calls",
                "Total number of impression events");

        public UnleashFilter(ILogger<UnleashFilter> logger, IUnleash unleash, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _unleash = unleash;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            bool isEnabled;
            try
            {
                var unleashContext =
                    _httpContextAccessor?.HttpContext?.Items[UnleashMiddlware.Unleashcontext] as
                        UnleashContext;
                isEnabled = _unleash.IsEnabled(context.FeatureName, unleashContext);
            }
            catch (Exception e)
            {
                _logger.LogError($"{nameof(EvaluateAsync)} has thrown an exception.", e);
                isEnabled = false;
            }

            _filterCounter.Add(1, new KeyValuePair<string, object>("featureName", context.FeatureName),
                new KeyValuePair<string, object>("isEnabled", isEnabled));
            return Task.FromResult(isEnabled);
        }
    }
}

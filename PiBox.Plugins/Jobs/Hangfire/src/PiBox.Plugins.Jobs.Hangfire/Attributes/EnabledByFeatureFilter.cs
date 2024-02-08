using Hangfire.Common;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace PiBox.Plugins.Jobs.Hangfire.Attributes
{
    /// <summary>
    /// Decide if to execute a job by enabled featured
    /// </summary>
    internal class EnabledByFeatureFilter : JobFilterAttribute, IServerFilter
    {
        private readonly IFeatureManager _featureManager;
        private readonly ILogger<EnabledByFeatureFilter> _logger;

        public EnabledByFeatureFilter(IFeatureManager featureManager, ILogger<EnabledByFeatureFilter> logger)
        {
            _featureManager = featureManager;
            _logger = logger;
            Order = 0;
        }

        public void OnPerforming(PerformingContext context)
        {
            var jobName = context.BackgroundJob.Job.Type.Name;
            if (_featureManager.IsEnabledAsync(jobName).Result) return;
            _logger.LogWarning("Execution of job {JobName} was cancelled due to not enabled feature {FeatureName}",
                jobName, jobName);
            context.Canceled = true;
        }

        public void OnPerformed(PerformedContext context)
        {
            // do nothing
        }
    }
}

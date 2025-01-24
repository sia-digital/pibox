using Hangfire.States;
using PiBox.Hosting.Abstractions.Attributes;

namespace PiBox.Plugins.Jobs.Hangfire
{
    [Configuration("hangfire")]
    public class HangfireConfiguration
    {
        public string AllowedDashboardHost { get; set; }
        public bool EnableJobsByFeatureManagementConfig { get; set; }
        public int? PollingIntervalInMs { get; set; }
        public int? WorkerCount { get; set; }
        public string[] Queues { get; set; } = [EnqueuedState.DefaultQueue];
    }
}

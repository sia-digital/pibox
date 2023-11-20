using PiBox.Hosting.Abstractions.Attributes;

namespace PiBox.Plugins.Management.Unleash
{
    [Configuration("unleash")]
    public class UnleashConfiguration
    {
        public string AppName { get; set; }
        public string ApiUri { get; set; }
        public string ApiToken { get; set; }
        public string ProjectId { get; set; }
        public string InstanceTag { get; set; }
        public string Environment { get; set; }
    }
}

using PiBox.Hosting.Abstractions.Attributes;

namespace PiBox.Plugins.Persistence.S3
{
    [Configuration("s3")]
    public class S3Configuration
    {
        public string Endpoint { get; set; } = "";
        public string AccessKey { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public string Region { get; set; } = "";
        public bool UseSsl { get; set; }
    }
}

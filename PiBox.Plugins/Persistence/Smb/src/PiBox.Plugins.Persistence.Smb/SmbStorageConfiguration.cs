using PiBox.Hosting.Abstractions.Attributes;

namespace PiBox.Plugins.Persistence.Smb
{
    [Configuration("smb")]
    public class SmbStorageConfiguration
    {
        public string Server { get; set; }
        public string Domain { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string DefaultShare { get; set; }
        public IList<PathMapping> ShareMappings { get; set; } = new List<PathMapping>();
        public IList<DriveMapping> DriveMappings { get; set; } = new List<DriveMapping>();

        public record PathMapping(string Folder, string Share);

        public record DriveMapping(string Drive, string Share);
    }
}

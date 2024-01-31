using FluentAssertions;
using NUnit.Framework;
using PiBox.Testing.Assertions;

namespace PiBox.Plugins.Persistence.Smb.Tests
{
    public class SmbStorageTests
    {
        private SmbStorage _smbStorage;
        private SmbStorageConfiguration _smbStorageConfiguration;
        private string ServerPath => $"{_smbStorageConfiguration.Server}";
        private string SharePath => $@"{ServerPath}\{_smbStorageConfiguration.DefaultShare}";
        private string GetCustomSharePath(string share) => $@"{ServerPath}\{share}";

        [SetUp]
        public void Setup()
        {
            _smbStorageConfiguration = new SmbStorageConfiguration
            {
                Username = "user",
                Password = "pass",
                Domain = "domain",
                Server = "localhost",
                DefaultShare = "some-share",
                ShareMappings = new List<SmbStorageConfiguration.PathMapping>(),
                DriveMappings = new List<SmbStorageConfiguration.DriveMapping>()
            };
            _smbStorage = new SmbStorage(_smbStorageConfiguration, new FakeLogger<SmbStorage>());
        }

        [Test]
        public async Task GetSmbPathReplacesPathDelimiters()
        {
            var path = await _smbStorage.GetSmbPath(@"Temp/test\x.pdf");
            path.Should().Be($@"{SharePath}\Temp\test\x.pdf");
        }

        [Test]
        public async Task GetSmbPathDriveMappingsReplaces()
        {
            _smbStorageConfiguration.DriveMappings.Add(new("J:", "someNicerShare"));
            var path = await _smbStorage.GetSmbPath(@"J:/Temp/test\x.pdf");
            path.Should().Be($@"{GetCustomSharePath("someNicerShare")}\Temp\test\x.pdf");
        }

        [Test]
        public async Task GetSmbPathCanResolveFile()
        {
            var path = await _smbStorage.GetSmbPath("x.pdf");
            path.Should().Be($@"{SharePath}\x.pdf");
        }

        [Test]
        public async Task GetSmbPathCanResolveFolder()
        {
            var path = await _smbStorage.GetSmbPath(@"Temp\test\");
            path.Should().Be($@"{SharePath}\Temp\test");
        }

        [Test]
        public async Task GetSmbPathCanRemapShares()
        {
            _smbStorageConfiguration.ShareMappings.Add(new SmbStorageConfiguration.PathMapping("Temp", "ShareTEMP"));
            var path = await _smbStorage.GetSmbPath(@"Temp\test\");
            path.Should().Be($@"{ServerPath}\ShareTEMP\test");

            path = await _smbStorage.GetSmbPath(@"Temp\test\file.txt");
            path.Should().Be($@"{ServerPath}\ShareTEMP\test\file.txt");
        }

        [Test]
        public async Task GetSmbPathReturnsSharePathIfNoPathProvided()
        {
            var path = await _smbStorage.GetSmbPath();
            path.Should().Be(SharePath);
        }
    }
}

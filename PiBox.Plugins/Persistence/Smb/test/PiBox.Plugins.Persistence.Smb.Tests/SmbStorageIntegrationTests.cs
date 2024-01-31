using FluentAssertions;
using NUnit.Framework;
using PiBox.Plugins.Persistence.Abstractions;
using PiBox.Testing.Assertions;

namespace PiBox.Plugins.Persistence.Smb.Tests
{
    [Explicit]
    public class SmbStorageIntegrationTests
    {
        private ISmbStorage _smbStorage;

        [SetUp]
        public void Setup()
        {
            _smbStorage = new SmbStorage(new SmbStorageConfiguration
            {
                Server = "<server>",
                DefaultShare = "<share>",
                Password = "<password>",
                Username = "<username>",
                Domain = "<domain>",
                ShareMappings = new List<SmbStorageConfiguration.PathMapping>
                {
                    new("<folder>", "<share>")
                },
                DriveMappings = new List<SmbStorageConfiguration.DriveMapping>
                {
                    new("<drive>", "<share>")
                }
            }, new FakeLogger<SmbStorage>());
        }

        [Test]
        [TestCase("Temp/...")]
        public async Task CanReadSmbFiles(string path)
        {
            var content = await _smbStorage.ReadStringAsync(path);
            content.Should().NotBeNullOrEmpty();
        }

        [Test]
        [TestCase("Temp/..", "Hallo!")]
        public async Task CanWriteSmbFiles(string path, string content)
        {
            await _smbStorage.Invoking(x => x.WriteStringAsync(path, content)).Should().NotThrowAsync();
        }
    }
}

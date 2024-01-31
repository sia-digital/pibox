using System.Text;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Persistence.Smb.Tests
{
    public class FileStorageExtensionsTests
    {
        private ISmbStorage _smbStorage;

        [SetUp]
        public void Setup()
        {
            _smbStorage = Substitute.For<ISmbStorage>();
        }

        [Test]
        public async Task CanWriteStringAsync()
        {
            await _smbStorage.WriteStringAsync("path", "content");

            await _smbStorage.Received(1).WriteAsync(Arg.Is("path"),
                Arg.Is<byte[]>(b => Encoding.UTF8.GetString(b).Equals("content", StringComparison.Ordinal)),
                Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task CanWriteStreamAsync()
        {
            using var ms = new MemoryStream("content"u8.ToArray());
            await _smbStorage.WriteStreamAsync("path", ms);

            await _smbStorage.Received(1).WriteAsync(Arg.Is("path"),
                Arg.Is<byte[]>(b => Encoding.UTF8.GetString(b).Equals("content", StringComparison.Ordinal)));
        }

        [Test]
        public async Task CanReadStringAsync()
        {
            _smbStorage.ReadStreamAsync(Arg.Is("path"), Arg.Any<CancellationToken>())
                .Returns(new MemoryStream("content"u8.ToArray()));
            var content = await _smbStorage.ReadStringAsync("path");
            content.Should().Be("content");

            await _smbStorage.Received(1).ReadStreamAsync(Arg.Is("path"), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task CanReadBytesAsync()
        {
            _smbStorage.ReadStreamAsync(Arg.Is("path"), Arg.Any<CancellationToken>())
                .Returns(new MemoryStream("content"u8.ToArray()));
            var content = await _smbStorage.ReadBytesAsync("path");
            Encoding.UTF8.GetString(content).Should().Be("content");

            await _smbStorage.Received(1).ReadStreamAsync(Arg.Is("path"), Arg.Any<CancellationToken>());
        }
    }
}

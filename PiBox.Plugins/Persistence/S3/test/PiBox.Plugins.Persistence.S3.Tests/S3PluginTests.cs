using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Minio;
using NSubstitute;
using NUnit.Framework;
using PiBox.Plugins.Persistence.Abstractions;
using PiBox.Testing;

namespace PiBox.Plugins.Persistence.S3.Tests
{
    [TestFixture]
    public class S3PluginTests
    {
        private readonly S3Configuration _configuration = new()
        {
            Endpoint = "s3.endpoint.de",
            AccessKey = "access",
            SecretKey = "secret",
            Region = "region",
            UseSsl = true
        };
        private S3Plugin _plugin = null!;

        [SetUp]
        public void Init()
        {
            _plugin = new S3Plugin(_configuration);
        }

        [Test]
        public void PluginConfiguresServices()
        {
            var sc = TestingDefaults.ServiceCollection();
            _plugin.ConfigureServices(sc);
            var sp = sc.BuildServiceProvider();

            var client = sp.GetRequiredService<IMinioClient>();
            client.Should().NotBeNull();
            var baseUrl = client.Config.BaseUrl;
            baseUrl.Should().NotBeNull();
            baseUrl.Should().Be(_configuration.Endpoint);
            var accessKey = client.Config.AccessKey;
            accessKey.Should().NotBeNull();
            accessKey.Should().Be(_configuration.AccessKey);
            var secretKey = client.Config.SecretKey;
            secretKey.Should().NotBeNull();
            secretKey.Should().Be(_configuration.SecretKey);
            var region = client.Config.Region;
            region.Should().NotBeNull();
            region.Should().Be(_configuration.Region);
            var secure = client.Config.Secure;
            secure.Should().Be(_configuration.UseSsl);

            var blobStorage = sp.GetRequiredService<IBlobStorage>();
            blobStorage.Should().NotBeNull();
            blobStorage.Should().BeOfType<S3BlobStorage>();
        }

        [Test]
        public void PluginConfiguresHealthChecks()
        {
            var healthChecksBuilder = Substitute.For<IHealthChecksBuilder>();
            healthChecksBuilder.Services.Returns(new ServiceCollection());
            _plugin.ConfigureHealthChecks(healthChecksBuilder);
            healthChecksBuilder.Received(1)
                .Add(Arg.Is<HealthCheckRegistration>(h => h.Name == "s3"));
        }
    }
}

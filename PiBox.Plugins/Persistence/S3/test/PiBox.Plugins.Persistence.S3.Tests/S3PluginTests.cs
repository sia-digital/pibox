using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Minio;
using NSubstitute;
using NUnit.Framework;
using PiBox.Plugins.Persistence.Abstractions;
using PiBox.Testing;
using PiBox.Testing.Extensions;

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

            var client = sp.GetRequiredService<MinioClient>();
            client.Should().NotBeNull();
            var baseUrl = client.GetInaccessibleValue<string>("BaseUrl");
            baseUrl.Should().NotBeNull();
            baseUrl.Should().Be(_configuration.Endpoint);
            var accessKey = client.GetInaccessibleValue<string>("AccessKey");
            accessKey.Should().NotBeNull();
            accessKey.Should().Be(_configuration.AccessKey);
            var secretKey = client.GetInaccessibleValue<string>("SecretKey");
            secretKey.Should().NotBeNull();
            secretKey.Should().Be(_configuration.SecretKey);
            var region = client.GetInaccessibleValue<string>("Region");
            region.Should().NotBeNull();
            region.Should().Be(_configuration.Region);
            var secure = client.GetInaccessibleValue<bool>("Secure");
            secure.Should().Be(_configuration.UseSsl);

            var blobStorage = sp.GetRequiredService<IBlobStorage>();
            blobStorage.Should().NotBeNull();
            blobStorage.Should().BeOfType<S3BlobStorage>();
        }

        [Test]
        public void PluginConfiguresHealthChecks()
        {
            var healthChecksBuilder = Substitute.For<IHealthChecksBuilder>();
            _plugin.ConfigureHealthChecks(healthChecksBuilder);
            healthChecksBuilder.Received(1)
                .Add(Arg.Is<HealthCheckRegistration>(h => h.Name == "s3"));
        }
    }
}

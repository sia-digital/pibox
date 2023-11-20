using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NUnit.Framework;
using PiBox.Testing.Assertions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace PiBox.Plugins.Messaging.Kafka.Flow.Tests
{
    public class KafkaHealthCheckTests
    {
        private const string SuccessBootstrapServer = "localhost:4443";
        private HealthCheckContext _healthCheckContext = default!;
        private WireMockServer _server = null!;
        private FakeLogger<KafkaHealthCheck> _fakeLogger = null!;

        private KafkaHealthCheck GetHealthCheck(string bootstrapServers = SuccessBootstrapServer)
        {
            var clientConfig = new ClientConfig { BootstrapServers = bootstrapServers };

            return new KafkaHealthCheck(clientConfig, _fakeLogger);
        }

        [SetUp]
        public void Init()
        {
            _fakeLogger = new FakeLogger<KafkaHealthCheck>();
            _server = WireMockServer.Start(4443);
            var serverRequest = Request.Create().UsingAnyMethod();
            _server.Given(serverRequest)
                .RespondWith(Response.Create().WithSuccess());
            _healthCheckContext = new HealthCheckContext();
        }

        [TearDown]
        public void Unload()
        {
            _server?.Dispose();
        }

        [Test]
        public async Task HealthyWhenServerIsUp()
        {
            var hc = GetHealthCheck();
            var result = await hc.CheckHealthAsync(_healthCheckContext, CancellationToken.None);
            result.Status.Should().Be(HealthStatus.Healthy);
            result.Description.Should().Be("Kafka is available.");
        }

        [Test]
        public async Task HealthyWhenOneServerIsUp()
        {
            var hc = GetHealthCheck($"localhost:4450,localhost:4460,{SuccessBootstrapServer}");
            var result = await hc.CheckHealthAsync(_healthCheckContext, CancellationToken.None);
            result.Status.Should().Be(HealthStatus.Healthy);
            result.Description.Should().Be("Kafka is available.");
        }

        [Test]
        public async Task UnhealthyWhenServersAreDown()
        {
            var hc = GetHealthCheck("localhost:4450,localhost:4460");
            var result = await hc.CheckHealthAsync(_healthCheckContext, CancellationToken.None);
            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Description.Should().StartWith("Kafka is unavailable.");
        }

        [Test]
        public async Task UnhealthyOnException()
        {
            var hc = GetHealthCheck("localhost:4450");
            var result = await hc.CheckHealthAsync(_healthCheckContext, CancellationToken.None);
            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Description.Should().StartWith("Kafka is unavailable.");
            _fakeLogger.Entries[0].Message.Should().Be("Kafka cluster member localhost:4450 is unavailable");
        }
    }
}

using Confluent.Kafka;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace PiBox.Plugins.Messaging.Kafka.Flow.Tests
{
    public class ConfigTests
    {
        [Test]
        public void DefaultConfigsShouldOverwriteClientConfig()
        {
            var clientConfig = Substitute.For<ClientConfig>();
            clientConfig.BootstrapServers = "localhost:9000";
            var consumerConfig = Substitute.For<ConsumerConfig>();
            consumerConfig.BootstrapServers = "localhost:9092";
            var producerConfig = Substitute.For<ProducerConfig>();
            producerConfig.BootstrapServers = "localhost:9093";

            var defaultConsumerConfig =
                DefaultConsumerConfig.WithDefaults(consumerConfig, clientConfig);
            defaultConsumerConfig.BootstrapServers.Should().Be("localhost:9092");
            defaultConsumerConfig.BootstrapServers.Should().NotBe("localhost:9000");

            var defaultProducerConfig = DefaultProducerConfig.WithDefaults(producerConfig, clientConfig);
            defaultProducerConfig.BootstrapServers.Should().Be("localhost:9093");
            defaultConsumerConfig.BootstrapServers.Should().NotBe("localhost:9000");
        }
    }
}

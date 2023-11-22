using Confluent.Kafka;
using Confluent.SchemaRegistry;
using FluentAssertions;
using KafkaFlow.Producers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using PiBox.Testing;

namespace PiBox.Plugins.Messaging.Kafka.Flow.Tests
{
    public class KafkaFlowServiceExtensionsTests
    {
        [Test]
        public void Works()
        {
            IServiceCollection serviceCollection = TestingDefaults.ServiceCollection();
            var configuration = CustomConfiguration.Create()
                    .Add("kafka:client:bootstrapServers", "localhost:9092")
                    .Add("kafka:client:saslMechanism", "plain")
                    .Add("kafka:client:saslUsername", "test-user")
                    .Add("kafka:client:saslPassword", "test-password")
                    .Add("kafka:client:securityProtocol", "SaslPlaintext")
                    .Add("kafka:client:sslCaLocation", "/tmp/cert.crt")
                    .Add("kafka:consumerDefaults:groupId", "a-nice-group")
                    .Add("kafka:producerDefaults:transactionTimeoutMs", "3000")
                    .Add("kafka:producerDefaults:compressionType", "gzip")
                    .Add("kafka:schemaRegistry:url", "localhost:8081")
                    .Add("kafka:schemaRegistry:basicAuthUserInfo", "admin:secret")
                    .Add("kafka:schemaRegistry:sslCaLocation", "/tmp/cert.crt")
                    .Add("kafka:schemaRegistry:enableSslCertificateVerification", "false")
                ;

            serviceCollection.ConfigureKafka(configuration.Build(), NullLogger.Instance, builder =>
                builder.AddConsumer<SampleConsumer>("sample-topic", "sample-group-id")
                    .AddProducer<SampleMessage>("sample-topic").AddTypedProducer<SampleMessage>("typed-topic"));
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var schemaRegistryConfig = serviceProvider.GetRequiredService<SchemaRegistryConfig>();
            schemaRegistryConfig.Url.Should().Be("localhost:8081");
            schemaRegistryConfig.BasicAuthUserInfo.Should().Be("admin:secret");
            schemaRegistryConfig.SslCaLocation.Should().Be("/tmp/cert.crt");
            schemaRegistryConfig.EnableSslCertificateVerification.Should().BeFalse();
            var clientConfig = serviceProvider.GetRequiredService<ClientConfig>();
            clientConfig.BootstrapServers.Should().Be("localhost:9092");
            var consumerConfig = serviceProvider.GetRequiredService<DefaultConsumerConfig>();
            consumerConfig.GroupId.Should().Be("a-nice-group");
            var producerConfig = serviceProvider.GetRequiredService<DefaultProducerConfig>();
            producerConfig.CompressionType.Should().Be(CompressionType.Gzip);
            producerConfig.TransactionTimeoutMs.Should().Be(3000);
            var producerAccessor = serviceProvider.GetRequiredService<IProducerAccessor>();
            producerAccessor.GetProducer("SampleMessage").Should().NotBeNull();
            producerAccessor.GetProducer<SampleMessage>().Should().NotBeNull();
        }
    }
}

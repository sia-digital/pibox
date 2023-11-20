using Confluent.Kafka;
using Confluent.SchemaRegistry;
using KafkaFlow;
using KafkaFlow.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PiBox.Hosting.Abstractions.Extensions;
using SaslMechanism = KafkaFlow.Configuration.SaslMechanism;
using SecurityProtocol = KafkaFlow.Configuration.SecurityProtocol;

namespace PiBox.Plugins.Messaging.Kafka.Flow
{
    public static class KafkaFlowServiceExtensions
    {
        public static void ConfigureKafka(this IServiceCollection serviceCollection,
            IConfiguration configuration,
            ILogger logger,
            Action<KafkaFlowBuilder> configure)
        {
            var (clientConfig, schemaRegistryConfig, consumerConfig, producerConfig) =
                RegisterConfigs(serviceCollection, configuration, logger);

            serviceCollection.AddKafkaFlowHostedService(kafkaConfigurationBuilder =>
                kafkaConfigurationBuilder.UseMicrosoftLog()
                    .AddCluster(clusterConfigurationBuilder =>
                    {
                        clusterConfigurationBuilder
                            .WithBrokers(clientConfig.BootstrapServers.Split(","))
                            .When(!string.IsNullOrEmpty(schemaRegistryConfig.Url),
                                c => c.WithSchemaRegistry(x => ConfigureSchemaRegistry(x, schemaRegistryConfig)))
                            .WithSecurityInformation(security => ApplySecurityConfig(clientConfig, security));
                        var builder = new KafkaFlowBuilder(consumerConfig, producerConfig, logger);
                        configure(builder);
                        builder.Build(clusterConfigurationBuilder);
                    })
            );
            serviceCollection.AddHostedService<KafkaFlowStatisticsMetricsReporter>();
        }

        private static void ConfigureSchemaRegistry(SchemaRegistryConfig configToKafka, SchemaRegistryConfig configFromEnvironment)
        {
            configToKafka.Url = configFromEnvironment.Url;
            if (!string.IsNullOrEmpty(configFromEnvironment.BasicAuthUserInfo))
            {
                configToKafka.SslCaLocation = configFromEnvironment.SslCaLocation;
                configToKafka.BasicAuthUserInfo = configFromEnvironment.BasicAuthUserInfo;
                configToKafka.EnableSslCertificateVerification = configFromEnvironment.EnableSslCertificateVerification;
            }
        }

        private static void ApplySecurityConfig(ClientConfig clientConfig, SecurityInformation security)
        {
            if (!string.IsNullOrEmpty(clientConfig.SaslMechanism.ToString()))
            {
                security.SaslMechanism =
                    Enum.Parse<SaslMechanism>(
                        clientConfig.SaslMechanism.ToString()!);
            }

            if (!string.IsNullOrEmpty(clientConfig.SaslUsername))
            {
                security.SaslUsername = clientConfig.SaslUsername;
            }

            if (!string.IsNullOrEmpty(clientConfig.SaslPassword))
            {
                security.SaslPassword = clientConfig.SaslPassword;
            }

            if (!string.IsNullOrEmpty(clientConfig.SecurityProtocol.ToString()))
            {
                security.SecurityProtocol =
                    Enum.Parse<SecurityProtocol>(
                        clientConfig.SecurityProtocol.ToString()!);
            }

            if (!string.IsNullOrEmpty(clientConfig.SslCaLocation))
            {
                security.SslCaLocation = clientConfig.SslCaLocation;
            }
        }

        private static (ClientConfig clientConfig, SchemaRegistryConfig schemaRegistryConfig, DefaultConsumerConfig
            consumerConfig, DefaultProducerConfig producerConfig) RegisterConfigs(IServiceCollection serviceCollection,
                IConfiguration configuration, ILogger logger)
        {
            var clientConfig = configuration.BindToSection<ClientConfig>("kafka:client");
            LogKafkaConfig(logger, "Found kafka client config", clientConfig.AsEnumerable());
            serviceCollection.AddSingleton(clientConfig);

            var schemaRegistryConfig = configuration.BindToSection<SchemaRegistryConfig>("kafka:schemaRegistry");
            LogKafkaConfig(logger, "Found kafka schema registry config", schemaRegistryConfig.AsEnumerable());
            serviceCollection.AddSingleton(schemaRegistryConfig);

            var consumerConfig =
                DefaultConsumerConfig.WithDefaults(
                    configuration.BindToSection<ConsumerConfig>("kafka:consumerDefaults"), clientConfig);
            LogKafkaConfig(logger, "Found kafka default consumer config", consumerConfig.AsEnumerable());
            serviceCollection.AddSingleton(consumerConfig);

            var producerConfig =
                DefaultProducerConfig.WithDefaults(
                    configuration.BindToSection<ProducerConfig>("kafka:producerDefaults"), clientConfig);
            LogKafkaConfig(logger, "Found kafka default producer config", producerConfig.AsEnumerable());
            serviceCollection.AddSingleton(producerConfig);
            return (clientConfig, schemaRegistryConfig, consumerConfig, producerConfig);
        }

        private static void LogKafkaConfig(ILogger logger, string message,
            IEnumerable<KeyValuePair<string, string>> clientConfig)
        {
            logger?.LogDebug("{Message} {KafkaConfig}", message,
                clientConfig
                    .Where(x => !x.Key.Contains("password", StringComparison.OrdinalIgnoreCase))
                    .ToArray());
        }
    }
}

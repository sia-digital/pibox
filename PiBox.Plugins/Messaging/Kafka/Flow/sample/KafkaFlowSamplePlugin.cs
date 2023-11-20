using PiBox.Hosting.Abstractions.Plugins;
using SchemaRegistry;

namespace PiBox.Plugins.Messaging.Kafka.Flow.Sample
{
    public class KafkaFlowSamplePlugin : IPluginServiceConfiguration
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public KafkaFlowSamplePlugin(IConfiguration configuration, ILogger<KafkaFlowSamplePlugin> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.ConfigureKafka(_configuration, _logger, kafkaFlowBuilder => kafkaFlowBuilder
                .AddProducer<ProtobufLogMessage>("protobuf-topic")
                .AddConsumer<ProtobufMessageHandler>("protobuf-topic", "mygroup")
                .AddConsumerWithDeadLetter<ProtobufDltMessageHandler, ProtobufLogMessage, ProtobufLogMessage>(
                    "protobuf-topic", "protobuf-dlt-topic", "mygroup"));

            serviceCollection.AddHostedService<SampleProducer>();
            serviceCollection.AddHostedService<SampleProducer2>();
        }
    }
}

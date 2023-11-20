using KafkaFlow;
using KafkaFlow.Producers;
using SchemaRegistry;

namespace PiBox.Plugins.Messaging.Kafka.Flow.Sample
{
    public class SampleProducer : BackgroundService
    {
        private readonly IMessageProducer _producer;

        public SampleProducer(IProducerAccessor producerAccessor)
        {
            _producer = producerAccessor.GetProducer("ProtobufLogMessage");

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _producer.ProduceAsync("protobuf-topic", Guid.NewGuid().ToString(),
                    new ProtobufLogMessage { Code = 1, Message = "Msg" });

                await Task.Delay(3000, stoppingToken);
            }
        }
    }
}

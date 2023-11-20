using KafkaFlow;
using KafkaFlow.Producers;
using SchemaRegistry;

namespace PiBox.Plugins.Messaging.Kafka.Flow.Sample
{
    public class SampleProducer2 : BackgroundService
    {
        private readonly IMessageProducer _producer;

        public SampleProducer2(IProducerAccessor producerAccessor)
        {
            _producer = producerAccessor.GetProducer("ProtobufLogMessage");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _producer.ProduceAsync("protobuf-topic-2", Guid.NewGuid().ToString(),
                    new ProtobufLogMessage { Code = 2, Message = "Msg2" });

                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}

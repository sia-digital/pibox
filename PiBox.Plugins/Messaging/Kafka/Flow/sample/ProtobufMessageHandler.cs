using KafkaFlow;
using KafkaFlow.TypedHandler;
using SchemaRegistry;

namespace PiBox.Plugins.Messaging.Kafka.Flow.Sample
{
    public class ProtobufMessageHandler : IMessageHandler<ProtobufLogMessage>
    {
        public Task Handle(IMessageContext context, ProtobufLogMessage message)
        {
            Console.WriteLine(
                "Partition: {0} | Offset: {1} | Message: {2} | Protobuf",
                context.ConsumerContext.Partition,
                context.ConsumerContext.Offset,
                message.Message);

            return Task.CompletedTask;
        }
    }
}

using KafkaFlow;
using KafkaFlow.TypedHandler;

namespace PiBox.Plugins.Messaging.Kafka.Flow.Tests
{
    public class SampleConsumer : IMessageHandler<SampleMessage>
    {
        public Task Handle(IMessageContext context, SampleMessage message)
        {
            return Task.CompletedTask;
        }
    }
}

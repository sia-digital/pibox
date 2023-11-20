using KafkaFlow;
using PiBox.Plugins.Messaging.Kafka.Flow.Handlers;
using SchemaRegistry;

namespace PiBox.Plugins.Messaging.Kafka.Flow.Sample
{
    public class ProtobufDltMessageHandler : DltMessageHandler<ProtobufLogMessage, ProtobufLogMessage>
    {
        public ProtobufDltMessageHandler(IMessageProducer<ProtobufLogMessage> deadLetterMessageProducer, ILogger logger
             ) : base(deadLetterMessageProducer, logger)
        {
        }

        protected override Task ProcessMessageAsync(IMessageContext context, ProtobufLogMessage message)
        {
            throw new Exception("test");
        }

        protected override ProtobufLogMessage HandleError(IMessageContext context, ProtobufLogMessage message,
            Exception error)
        {
            return new ProtobufLogMessage { Code = 1, Message = "Dlt sample error" };
        }
    }
}

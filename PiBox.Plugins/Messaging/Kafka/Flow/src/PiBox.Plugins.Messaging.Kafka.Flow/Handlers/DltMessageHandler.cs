using System.Diagnostics.Metrics;
using KafkaFlow;
using KafkaFlow.TypedHandler;
using Microsoft.Extensions.Logging;
using PiBox.Hosting.Abstractions.Metrics;

namespace PiBox.Plugins.Messaging.Kafka.Flow.Handlers
{
    public abstract class DltMessageHandler<TMessage, TDeadLetterMessage> : IMessageHandler<TMessage>
    {
        internal Counter<long> _processedMessagesSuccessCounter = Metrics.CreateCounter<long>("kafka_messages_success_count_total", "calls", "count of logged successful messages");
        internal Counter<long> _processedMessagesFailedCounter = Metrics.CreateCounter<long>("kafka_messages_failed_count_total", "calls", "count of logged failed messages");

        private readonly IMessageProducer<TDeadLetterMessage> _deadLetterMessageProducer;
        private readonly ILogger _logger;

        protected DltMessageHandler(IMessageProducer<TDeadLetterMessage> deadLetterMessageProducer, ILogger logger)
        {
            _deadLetterMessageProducer = deadLetterMessageProducer;
            _logger = logger;
        }

        public async Task Handle(IMessageContext context, TMessage message)
        {
            var tags = new[] { new KeyValuePair<string, object>("label", context.ConsumerContext.Topic) };
            _logger.LogDebug("Partition: {Partition} | Offset: {Offset} | Type: {Type} | Message: {Message}",
                context.ConsumerContext.Partition,
                context.ConsumerContext.Offset,
                typeof(TMessage).Name,
                message);
            try
            {
                await ProcessMessageAsync(context, message);
                _processedMessagesSuccessCounter.Add(1, tags);
            }
            catch (Exception e)
            {
                await WriteToDlt(context, message, e);
                _processedMessagesFailedCounter.Add(1, tags);
            }
        }

        private async Task WriteToDlt(IMessageContext context, TMessage message, Exception exception)
        {
            _logger.LogError(exception, "Partition: {Partition} | Offset: {Offset} | Type: {Type} | Message: {Message} | Exception: {Exception}",
                context.ConsumerContext.Partition,
                context.ConsumerContext.Offset,
                typeof(TMessage).Name,
                message,
                string.Join("#", exception.Message, exception.StackTrace));
            var deadLetterMessage = HandleError(context, message, exception);
            await _deadLetterMessageProducer.ProduceAsync(context.Message.Key, deadLetterMessage);
        }

        protected abstract Task ProcessMessageAsync(IMessageContext context, TMessage message);

        protected abstract TDeadLetterMessage HandleError(IMessageContext context, TMessage message, Exception exception);
    }
}

using KafkaFlow;
using Microsoft.Extensions.Logging;

namespace PiBox.Plugins.Messaging.Kafka.Flow
{
    public class KafkaDebugLoggingMiddleware : IMessageMiddleware
    {
        private readonly ILogger<KafkaDebugLoggingMiddleware> _logger;

        public KafkaDebugLoggingMiddleware(ILogger<KafkaDebugLoggingMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task Invoke(IMessageContext context, MiddlewareDelegate next)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                int? partition = null;
                long? offset = null;
                if (!string.IsNullOrEmpty(context.ConsumerContext?.GroupId))
                {
                    partition = context.ConsumerContext.Partition;
                    offset = context.ConsumerContext.Offset;
                }

                if (!string.IsNullOrEmpty(context.ProducerContext?.Topic))
                {
                    partition = context.ProducerContext.Partition;
                    offset = context.ProducerContext.Offset;
                }

                _logger.LogDebug("Partition: {Partition} | Offset: {Offset} | MsgKey: {MessageKey} | MsgValue: {MessageValue}",
                    partition,
                    offset,
                    context.Message.Key,
                    context.Message.Value);
            }

            await next(context);
        }
    }
}

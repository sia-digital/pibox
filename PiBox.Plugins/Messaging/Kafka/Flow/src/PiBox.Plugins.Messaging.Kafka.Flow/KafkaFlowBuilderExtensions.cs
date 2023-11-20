using KafkaFlow.Configuration;

namespace PiBox.Plugins.Messaging.Kafka.Flow
{
    public static class KafkaFlowBuilderExtensions
    {
        public static IConsumerConfigurationBuilder When(this IConsumerConfigurationBuilder builder, bool predicate,
            Action<IConsumerConfigurationBuilder> action)
        {
            if (predicate) action(builder);
            return builder;
        }

        public static IClusterConfigurationBuilder When(this IClusterConfigurationBuilder builder, bool predicate,
            Action<IClusterConfigurationBuilder> action)
        {
            if (predicate) action(builder);
            return builder;
        }
    }
}

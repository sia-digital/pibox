using Confluent.Kafka;

namespace PiBox.Plugins.Messaging.Kafka.Flow
{
    public class DefaultConsumerConfig : ConsumerConfig
    {
        public static DefaultConsumerConfig WithDefaults(ConsumerConfig consumerConfig, ClientConfig clientConfig)
        {
            var dictionary = new Dictionary<string, string>(clientConfig);
            foreach (var (key, value) in consumerConfig)
            {
                dictionary[key] = value;
            }

            return new(dictionary);
        }

        public DefaultConsumerConfig(IDictionary<string, string> dictionary) : base(dictionary)
        {
        }
    }
}

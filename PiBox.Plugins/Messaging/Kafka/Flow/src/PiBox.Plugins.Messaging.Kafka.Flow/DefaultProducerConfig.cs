using Confluent.Kafka;

namespace PiBox.Plugins.Messaging.Kafka.Flow
{
    public class DefaultProducerConfig : ProducerConfig
    {
        public static DefaultProducerConfig WithDefaults(ProducerConfig producerConfig, ClientConfig clientConfig)
        {
            var dictionary = new Dictionary<string, string>(clientConfig);

            foreach (var (key, value) in producerConfig)
            {
                dictionary[key] = value;
            }

            return new(dictionary);
        }

        public DefaultProducerConfig(IDictionary<string, string> dictionary) : base(dictionary)
        {
        }
    }
}

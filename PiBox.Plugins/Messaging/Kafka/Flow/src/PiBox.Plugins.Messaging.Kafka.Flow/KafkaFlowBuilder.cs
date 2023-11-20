using System.Diagnostics;
using System.Reflection;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using KafkaFlow;
using KafkaFlow.Configuration;
using KafkaFlow.TypedHandler;
using Microsoft.Extensions.Logging;
using PiBox.Plugins.Messaging.Kafka.Flow.Handlers;
using AutoOffsetReset = KafkaFlow.AutoOffsetReset;

namespace PiBox.Plugins.Messaging.Kafka.Flow
{
    public class KafkaFlowBuilder
    {
        private readonly ConsumerConfig _consumerDefaults;
        private readonly ProducerConfig _producerDefaults;
        private readonly ILogger _logger;

        private readonly IList<Action<IConsumerConfigurationBuilder>> _consumers =
            new List<Action<IConsumerConfigurationBuilder>>();

        private readonly IList<(string, Action<IProducerConfigurationBuilder>)> _producers =
            new List<(string, Action<IProducerConfigurationBuilder>)>();

        private readonly Dictionary<Type, Action<IProducerConfigurationBuilder>> _typedProducers = new();

        public KafkaFlowBuilder(ConsumerConfig consumerDefaults, ProducerConfig producerDefaults, ILogger logger)
        {
            _consumerDefaults = consumerDefaults;
            _producerDefaults = producerDefaults;
            _logger = logger;
        }

        private KafkaFlowBuilder AddConsumer<TMessageHandler>(string topic, string groupId, ConsumerConfig consumerConfig)
            where TMessageHandler : class, IMessageHandler
        {
            _consumers.Add(builder => builder
                .WithConsumerConfig(consumerConfig)
                .Topic(topic)
                .When(!string.IsNullOrEmpty(groupId), x => x.WithGroupId(groupId))
                .WithBufferSize(100)
                .WithWorkersCount(10)
                .WithAutoOffsetReset(AutoOffsetReset.Earliest)
                .AddMiddlewares(middlewares => middlewares
                    .AddAtBeginning(f => new KafkaDebugLoggingMiddleware(f.Resolve<ILogger<KafkaDebugLoggingMiddleware>>()))
                    .AddSchemaRegistryProtobufSerializerWhichRespectsCsharpNamespaceDeclaration(_logger)
                    .AddTypedHandlers(handlers => handlers
                        .AddHandler<TMessageHandler>())
                ));
            return this;
        }

        public KafkaFlowBuilder AddConsumerWithDeadLetter<TMessageHandler, TMessage, TDeadLetterMessage>(string topic, string deadLetterTopic, string groupId)
            where TMessageHandler : DltMessageHandler<TMessage, TDeadLetterMessage>
        {
            AddConsumer<TMessageHandler>(topic, groupId, _consumerDefaults!);
            return AddTypedProducer<TDeadLetterMessage>(deadLetterTopic);
        }

        public KafkaFlowBuilder AddConsumer<TMessageHandler>(string topic, string groupId)
            where TMessageHandler : class, IMessageHandler
        {
            return AddConsumer<TMessageHandler>(topic, groupId, _consumerDefaults!);
        }

        public KafkaFlowBuilder AddProducer<TProducer>(string topic, ProducerConfig producerConfig)
        {
            _producers.Add((typeof(TProducer).Name, builder => builder
                .WithProducerConfig(producerConfig)
                .DefaultTopic(topic)
                .WithCompression(CompressionType.Gzip)
                .AddMiddlewares(middlewares => middlewares
                    .AddAtBeginning(f => new KafkaDebugLoggingMiddleware(f.Resolve<ILogger<KafkaDebugLoggingMiddleware>>()))
                    .AddSchemaRegistryProtobufSerializerWhichRespectsCsharpNamespaceDeclaration(_logger,
                        new ProtobufSerializerConfig { SubjectNameStrategy = SubjectNameStrategy.Topic, AutoRegisterSchemas = true })
                )));
            return this;
        }

        public KafkaFlowBuilder AddProducer<TProducer>(string topic)
        {
            return AddProducer<TProducer>(topic, _producerDefaults!);
        }

        public KafkaFlowBuilder AddTypedProducer<TMessage>(string topic, ProducerConfig producerConfig)
        {
            _typedProducers.Add(typeof(TMessage), builder => builder
                .WithProducerConfig(producerConfig)
                .DefaultTopic(topic)
                .WithCompression(CompressionType.Gzip)
                .AddMiddlewares(middlewares => middlewares
                    .AddAtBeginning(f => new KafkaDebugLoggingMiddleware(f.Resolve<ILogger<KafkaDebugLoggingMiddleware>>()))
                    .AddSchemaRegistryProtobufSerializerWhichRespectsCsharpNamespaceDeclaration(_logger,
                        new ProtobufSerializerConfig { SubjectNameStrategy = SubjectNameStrategy.Topic, AutoRegisterSchemas = true })
                ));
            return this;
        }

        public KafkaFlowBuilder AddTypedProducer<TMessage>(string topic)
        {
            return AddTypedProducer<TMessage>(topic, _producerDefaults!);
        }

        internal void Build(IClusterConfigurationBuilder clusterConfigurationBuilder)
        {
            foreach (var consumer in _consumers)
                clusterConfigurationBuilder.AddConsumer(consumer);

            foreach (var producer in _producers)
                clusterConfigurationBuilder.AddProducer(producer.Item1, producer.Item2);

            foreach (var typedProducer in _typedProducers)
            {
                var method = typeof(IClusterConfigurationBuilder).GetMethod(
                    nameof(clusterConfigurationBuilder.AddProducer), BindingFlags.Public | BindingFlags.Instance, null,
                    CallingConventions.Any, new[] { typeof(Action<IProducerConfigurationBuilder>) }, null);
                Debug.Assert(method != null, "AddProducer<> method could not be found.");
                method = method.MakeGenericMethod(typedProducer.Key);
                method.Invoke(clusterConfigurationBuilder, new object[] { typedProducer.Value });
            }
        }
    }
}

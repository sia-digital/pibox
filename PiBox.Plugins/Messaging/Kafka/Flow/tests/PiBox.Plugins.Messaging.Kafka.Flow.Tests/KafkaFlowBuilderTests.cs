using Confluent.Kafka;
using FluentAssertions;
using KafkaFlow;
using KafkaFlow.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using NUnit.Framework;
using PiBox.Plugins.Messaging.Kafka.Flow.Handlers;
using PiBox.Testing.Assertions;

// ReSharper disable ClassNeverInstantiated.Local

namespace PiBox.Plugins.Messaging.Kafka.Flow.Tests
{
    [TestFixture]
    public class KafkaFlowBuilderTests
    {
        [Test]
        public void EmptyBuilderAddsNoConsumersNorProducers()
        {
            var consumerConfig = Substitute.For<ConsumerConfig>();
            var producerConfig = Substitute.For<ProducerConfig>();
            var clusterConfigurationBuilder = Substitute.For<IClusterConfigurationBuilder>();
            var builder = new KafkaFlowBuilder(consumerConfig, producerConfig, new FakeLogger<KafkaFlowBuilder>());
            builder.Build(clusterConfigurationBuilder);
            clusterConfigurationBuilder.Should().NotBeNull();
            clusterConfigurationBuilder.Received(Quantity.None())
                .AddConsumer(Arg.Any<Action<IConsumerConfigurationBuilder>>())
                .AddProducer(Arg.Any<string>(), Arg.Any<Action<IProducerConfigurationBuilder>>());
        }

        [Test]
        public void BuilderCanAddDltMessageConsumers()
        {
            var consumerConfig = Substitute.For<ConsumerConfig>();
            var producerConfig = Substitute.For<ProducerConfig>();
            var clusterConfigurationBuilder = Substitute.For<IClusterConfigurationBuilder>();

            var builder = new KafkaFlowBuilder(consumerConfig, producerConfig, new FakeLogger<KafkaFlowBuilder>());
            builder.AddConsumerWithDeadLetter<SampleDltHandler, Message, DltMessage>("correct", "fails", "group");
            builder.Build(clusterConfigurationBuilder);

            clusterConfigurationBuilder.Should().NotBeNull();
            clusterConfigurationBuilder.AddConsumer(Arg.Any<Action<IConsumerConfigurationBuilder>>())
                .Received(Quantity.Exactly(1));
            clusterConfigurationBuilder
                .AddProducer(Arg.Is<string>(s => s == "fails"), Arg.Any<Action<IProducerConfigurationBuilder>>())
                .Received(Quantity.Exactly(1));
        }

        private record Message;

        private record DltMessage;

        private class SampleDltHandler : DltMessageHandler<Message, DltMessage>
        {
            public SampleDltHandler(IMessageProducer<DltMessage> deadLetterMessageProducer, ILogger logger) : base(deadLetterMessageProducer, logger)
            {
            }

            protected override Task ProcessMessageAsync(IMessageContext context, Message message)
            {
                throw new NotSupportedException();
            }

            protected override DltMessage HandleError(IMessageContext context, Message message, Exception error)
            {
                throw new NotSupportedException();
            }
        }

        [Test]
        public void BuilderAddsOneConsumerAndOneProducer()
        {
            var consumerConfig = Substitute.For<ConsumerConfig>();
            var producerConfig = Substitute.For<ProducerConfig>();
            var clusterConfigurationBuilder = Substitute.For<IClusterConfigurationBuilder>();

            var builder = new KafkaFlowBuilder(consumerConfig, producerConfig, new FakeLogger<KafkaFlowBuilder>());
            builder.AddConsumer<SampleConsumer>("sample-topic", "sample-group-id");
            builder.AddProducer<SampleMessage>("sample-topic");
            builder.Build(clusterConfigurationBuilder);

            clusterConfigurationBuilder.Should().NotBeNull();
            clusterConfigurationBuilder.AddConsumer(Arg.Any<Action<IConsumerConfigurationBuilder>>())
                .Received(Quantity.Exactly(1));
            clusterConfigurationBuilder
                .AddProducer(Arg.Any<string>(), Arg.Any<Action<IProducerConfigurationBuilder>>())
                .Received(Quantity.Exactly(1));
        }

        [Test]
        public void WhenIClusterConfigurationBuilderExtensionIsOnlyExecutedOnTruePredicate()
        {
            var builder = Substitute.For<IClusterConfigurationBuilder>();

            builder.When(true, configurationBuilder => configurationBuilder.WithName("my-cluster"));
            builder.Received(Quantity.Exactly(1)).WithName("my-cluster");
            builder.Should().NotBeNull();
        }

        [Test]
        public void WhenIConsumerConfigurationBuilderExtensionIsOnlyExecutedOnTruePredicate()
        {
            var builder = Substitute.For<IConsumerConfigurationBuilder>();

            builder.When(true, configurationBuilder => configurationBuilder.WithName("my-consumer"));
            builder.Received(Quantity.Exactly(1)).WithName("my-consumer");
            builder.Should().NotBeNull();
        }

        [Test]
        public void BuilderAddsOneTypedProducer()
        {
            var consumerConfig = Substitute.For<ConsumerConfig>();
            var producerConfig = Substitute.For<ProducerConfig>();
            var clusterConfigurationBuilder = Substitute.For<IClusterConfigurationBuilder>();

            var builder = new KafkaFlowBuilder(consumerConfig, producerConfig, new FakeLogger<KafkaFlowBuilder>());
            builder.AddTypedProducer<SampleMessage>("topic");
            builder.Build(clusterConfigurationBuilder);

            clusterConfigurationBuilder.Should().NotBeNull();
            clusterConfigurationBuilder.AddProducer<SampleMessage>(Arg.Any<Action<IProducerConfigurationBuilder>>())
                .Received(Quantity.Exactly(1));
        }
    }
}

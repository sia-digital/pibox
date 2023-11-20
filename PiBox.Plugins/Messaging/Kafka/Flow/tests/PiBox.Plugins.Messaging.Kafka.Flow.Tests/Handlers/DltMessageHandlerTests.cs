using FluentAssertions;
using KafkaFlow;
using KafkaFlow.TypedHandler;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using PiBox.Plugins.Messaging.Kafka.Flow.Handlers;
using PiBox.Testing;
using PiBox.Testing.Assertions;

namespace PiBox.Plugins.Messaging.Kafka.Flow.Tests.Handlers
{
    [TestFixture]
    public class DltMessageHandlerTests
    {
        private const string KafkaTopic = "sample-topic";
        private FakeLogger<SampleHandler> _logger = null!;
        private IWorkUnit _unit = null!;
        private IMessageProducer<DeadLetterMessage> _deadLetterProducer = null!;
        private IMessageContext _messageContext = null!;
        private IMessageHandler<Message> _handler = null!;

        [SetUp]
        public void Initialize()
        {
            _logger = new FakeLogger<SampleHandler>();
            _unit = Substitute.For<IWorkUnit>();

            _deadLetterProducer = Substitute.For<IMessageProducer<DeadLetterMessage>>();
            _messageContext = Substitute.For<IMessageContext>();
            _messageContext.ConsumerContext.Topic.Returns(KafkaTopic);
            _handler = new SampleHandler(_deadLetterProducer, _logger, _unit);
        }

        [Test]
        public async Task CanHandleMessagesSuccess()
        {
            var metricsCollector = new TestMetricsCollector("kafka_messages_success_count_total");
            var sampleMessage = new Message { Age = 33 };
            await _handler.Handle(_messageContext, sampleMessage);
            _logger.Entries.Should().HaveCount(1);
            var logMessage = _logger.Entries.First();
            logMessage.Level.Should().Be(LogLevel.Debug);
            logMessage.Message.Should().Contain($"| Type: {nameof(Message)}");
            _unit.Received(1).DoTheJob();

            metricsCollector.Instruments.Should().Contain("kafka_messages_success_count_total");
            metricsCollector.GetSum().Should().Be(1);
            metricsCollector.CollectedMetrics.Should().ContainsMetric(1);
        }

        [Test]
        public async Task CanHandleMessagesError()
        {
            var metricsCollector = new TestMetricsCollector("kafka_messages_failed_count_total");
            var sampleMessage = new Message { Age = 33 };
            _unit.When(x => x.DoTheJob()).Throw(new Exception("Failure, this is a unit test exception"));
            await _handler.Handle(_messageContext, sampleMessage);
            _logger.Entries.Should().HaveCount(2);
            var logMessage = _logger.Entries.Last();
            logMessage.Level.Should().Be(LogLevel.Error);
            logMessage.Message.Should().Contain($"| Type: {nameof(Message)}")
                .And.Contain("Failure, this is a unit test exception").And.Contain("at PiBox.Plugins.Messaging.Kafka.Flow.Tests.Handlers.SampleHandler.ProcessMessageAsync(IMessageContext context, Message message)");
            _unit.Received(1).DoTheJob();

            metricsCollector.Instruments.Should().Contain("kafka_messages_failed_count_total");
            metricsCollector.GetSum().Should().Be(1);
            metricsCollector.CollectedMetrics.Should().ContainsMetric(1);
            var call = _deadLetterProducer.ReceivedCalls().SingleOrDefault();
            call.Should().NotBeNull();
            var args = call!.GetArguments();
            args[0].Should().BeNull();
            args[1].Should().BeOfType<DeadLetterMessage>();
            var dlm = (DeadLetterMessage)args[1]!;
            dlm.OriginalMessage.Should().Be(sampleMessage);
            dlm.ExceptionMessage.Should().Contain("Failure, this is a unit test exception");
        }
    }

    public interface IWorkUnit
    {
        void DoTheJob();
    }

    public class Message
    {
        public int Age { get; set; }
    }

    public class DeadLetterMessage
    {
        public Message OriginalMessage { get; set; } = null!;
        public string ExceptionMessage { get; set; } = null!;
    }

    public class SampleHandler : DltMessageHandler<Message, DeadLetterMessage>
    {
        private readonly IWorkUnit _unit;
        public SampleHandler(IMessageProducer<DeadLetterMessage> deadLetterMessageProducer, ILogger logger, IWorkUnit unit) : base(deadLetterMessageProducer, logger)
        {
            _unit = unit;
        }

        protected override Task ProcessMessageAsync(IMessageContext context, Message message)
        {
            _unit.DoTheJob();
            return Task.CompletedTask;
        }

        protected override DeadLetterMessage HandleError(IMessageContext context, Message message, Exception exception)
        {
            return new DeadLetterMessage { ExceptionMessage = string.Join("#", exception.Message), OriginalMessage = message };
        }
    }
}

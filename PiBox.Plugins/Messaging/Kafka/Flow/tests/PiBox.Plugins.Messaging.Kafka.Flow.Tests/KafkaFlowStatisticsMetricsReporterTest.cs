using Confluent.Kafka;
using FluentAssertions;
using KafkaFlow.Consumers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using PiBox.Testing;
using PiBox.Testing.Assertions;

namespace PiBox.Plugins.Messaging.Kafka.Flow.Tests
{
    public class KafkaFlowStatisticsMetricsReporterTest
    {
        [Test]
        public void CollectDataSetsLogsException()
        {

            var logger = new FakeLogger<KafkaFlowStatisticsMetricsReporter>();
            var reporter = new KafkaFlowStatisticsMetricsReporter(null!, logger);
            reporter.CollectData(null!);
            logger.Entries.Should().HaveCount(1);
            logger.Entries[0].Message.Should().Be("Exception occured while gathering kafkaflow metrics");
            logger.Entries[0].Level.Should().Be(LogLevel.Error);
            logger.Entries[0].Exception.Should().BeOfType<NullReferenceException>();
        }

        [Test]
        public void CollectDataSetsTheCurrentGaugeValue()
        {
            var metricName = "kafkaflow_consumer";
            using var lagTotalMetric = new TestMetricsCollector(metricName + "_lag_total");
            lagTotalMetric.CollectedMetrics.Should().BeEmpty();

            using var partitionsRunningTotalMetric = new TestMetricsCollector(metricName + "_partitions_running_total");
            partitionsRunningTotalMetric.CollectedMetrics.Should().BeEmpty();

            using var partitionsPausedTotalMetric = new TestMetricsCollector(metricName + "_partitions_paused_total");
            partitionsPausedTotalMetric.CollectedMetrics.Should().BeEmpty();

            using var statusMetric = new TestMetricsCollector(metricName + "_status");
            statusMetric.CollectedMetrics.Should().BeEmpty();

            var consumerAccessor = Substitute.For<IConsumerAccessor>();
            var messageConsumer = Substitute.For<IMessageConsumer>();
            var testConsumer = "test-consumer";
            messageConsumer.ConsumerName.Returns(testConsumer);
            var testTopic = "test-topic";
            messageConsumer.Topics.Returns(new[] { testTopic });
            messageConsumer.PausedPartitions.Returns(new List<TopicPartition>
            {
                new TopicPartition(testTopic, new Partition(0))
            });
            messageConsumer.RunningPartitions.Returns(new List<TopicPartition>
            {
                new TopicPartition(testTopic, new Partition(1))
            });
            messageConsumer.GetTopicPartitionsLag()
                .Returns(new List<TopicPartitionLag> { new TopicPartitionLag(testTopic, 1, 13) });
            messageConsumer.Status.Returns(ConsumerStatus.Running);
            consumerAccessor.All.Returns(new List<IMessageConsumer> { messageConsumer });
            var reporter = new KafkaFlowStatisticsMetricsReporter(consumerAccessor,
                Substitute.For<ILogger<KafkaFlowStatisticsMetricsReporter>>());
            reporter.StartAsync(CancellationToken.None);
            reporter.CollectData(null!);
            reporter.StopAsync(CancellationToken.None);
            reporter.Dispose();

            lagTotalMetric.RecordObservableInstruments();
            lagTotalMetric.Instruments.Should().Contain(metricName + "_lag_total");
            lagTotalMetric.GetSum().Should().Be(13);
            lagTotalMetric.CollectedMetrics.Should().ContainsMetric(13, new[]
            {
                new KeyValuePair<string, string>("consumer","test-consumer"),
                new KeyValuePair<string, string>("topic","test-topic"),
            });

            partitionsRunningTotalMetric.RecordObservableInstruments();
            partitionsRunningTotalMetric.Instruments.Should().Contain(metricName + "_partitions_running_total");
            partitionsRunningTotalMetric.GetSum().Should().Be(1);
            partitionsRunningTotalMetric.CollectedMetrics.Should().ContainsMetric(1, new[]
            {
                new KeyValuePair<string, string>("consumer","test-consumer"),
                new KeyValuePair<string, string>("topic","test-topic"),
            });

            partitionsPausedTotalMetric.RecordObservableInstruments();
            partitionsPausedTotalMetric.Instruments.Should().Contain(metricName + "_partitions_paused_total");
            partitionsPausedTotalMetric.GetSum().Should().Be(1);
            partitionsPausedTotalMetric.CollectedMetrics.Should().ContainsMetric(1, new[]
            {
                new KeyValuePair<string, string>("consumer","test-consumer"),
                new KeyValuePair<string, string>("topic","test-topic"),
            });

            statusMetric.RecordObservableInstruments();
            statusMetric.Instruments.Should().Contain(metricName + "_status");
            statusMetric.GetSum().Should().Be(1);
            statusMetric.CollectedMetrics.Should().ContainsMetric(1, new[]
            {
                new KeyValuePair<string, string>("consumer","test-consumer"),
                new KeyValuePair<string, string>("topic","test-topic"),
                new KeyValuePair<string, string>("status",ConsumerStatus.Running.ToString()),
            });
        }
    }
}

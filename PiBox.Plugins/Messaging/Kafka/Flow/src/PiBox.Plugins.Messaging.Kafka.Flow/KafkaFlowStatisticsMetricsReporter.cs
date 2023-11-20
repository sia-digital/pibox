using System.Diagnostics.Metrics;
using KafkaFlow.Consumers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PiBox.Hosting.Abstractions.Metrics;

namespace PiBox.Plugins.Messaging.Kafka.Flow
{
    public sealed class KafkaFlowStatisticsMetricsReporter : IHostedService, IDisposable
    {
        private const string MetricName = "kafkaflow_consumer";
        private readonly IConsumerAccessor _consumerAccessor;
        private readonly ILogger<KafkaFlowStatisticsMetricsReporter> _logger;
        private Timer _timer = null!;
        private static IEnumerable<ConsumerMetrics> _consumerTelemetryMetrics = new List<ConsumerMetrics>();

        public KafkaFlowStatisticsMetricsReporter(
            IConsumerAccessor consumerAccessor,
            ILogger<KafkaFlowStatisticsMetricsReporter> logger)
        {
            _consumerAccessor = consumerAccessor;
            _logger = logger;
            SetupGauges();
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(CollectData!, null, 1000, 5000);
            return Task.CompletedTask;
        }

        private record ConsumerMetrics(string ConsumerName, string Topic, IEnumerable<int> PausedPartitions, IEnumerable<int> RunningPartitions, ConsumerStatus Status, long Lag)
        {
            public override string ToString()
            {
                return
                    $"{{ ConsumerName = {ConsumerName}, Topic = {Topic}, PausedPartitions = {PausedPartitions}, RunningPartitions = {RunningPartitions}, Status = {Status}, Lag = {Lag} }}";
            }
        }

        private static void SetupGauges()
        {
            Metrics.CreateObservableGauge($"{MetricName}_lag_total",
                () => _consumerTelemetryMetrics.Select(metric => new Measurement<long>(metric.Lag, new("consumer", metric.ConsumerName), new("topic", metric.Topic))).ToList(),
                "items", "description");

            Metrics.CreateObservableGauge($"{MetricName}_partitions_running_total",
                () => _consumerTelemetryMetrics
                    .Select(metric => new Measurement<long>(metric.RunningPartitions.Count(), new("consumer", metric.ConsumerName), new("topic", metric.Topic))).ToList(),
                "items", "description");

            Metrics.CreateObservableGauge($"{MetricName}_partitions_paused_total",
                () => _consumerTelemetryMetrics
                    .Select(metric => new Measurement<long>(metric.PausedPartitions.Count(), new("consumer", metric.ConsumerName), new("topic", metric.Topic))).ToList(),
                "items", "description");

            Metrics.CreateObservableGauge($"{MetricName}_status",
                () => _consumerTelemetryMetrics.Select(metric =>
                    new Measurement<long>(1, new("consumer", metric.ConsumerName), new("topic", metric.Topic), new("status", metric.Status.ToString()))).ToList(),
                "items", "description");
        }

        internal void CollectData(object state)
        {
            try
            {
                _consumerTelemetryMetrics = _consumerAccessor.All
                    .SelectMany(
                        c =>
                        {
                            var consumerLag = c.GetTopicPartitionsLag();

                            return c.Topics.Select(
                                topic => new ConsumerMetrics(c.ConsumerName, topic, c.PausedPartitions
                                    .Where(p => p.Topic == topic)
                                    .Select(p => p.Partition.Value), c.RunningPartitions
                                    .Where(p => p.Topic == topic)
                                    .Select(p => p.Partition.Value), c.Status, consumerLag.Where(l => l.Topic == topic).Sum(l => l.Lag)));
                        }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occured while gathering kafkaflow metrics");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            return Task.CompletedTask;
        }
    }
}

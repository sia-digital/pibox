using System.Collections.Concurrent;
using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PiBox.Hosting.Abstractions.Metrics;

namespace PiBox.Plugins.Jobs.Hangfire
{
    public sealed class HangfireStatisticsMetricsReporter : IHostedService, IDisposable
    {
        private const string MetricName = "hangfire_job_count";
        private readonly JobStorage _hangfireJobStorage;
        private static readonly ConcurrentDictionary<string, long> _metricValues = new();

        private readonly ILogger<HangfireStatisticsMetricsReporter> _logger;
        private Timer _timer = null!;

        public HangfireStatisticsMetricsReporter(JobStorage hangfireJobStorage,
            ILogger<HangfireStatisticsMetricsReporter> logger)
        {
            _hangfireJobStorage = hangfireJobStorage;
            _logger = logger;
            SetupGauges();
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(CollectData!, null, 1000, 60000);
            return Task.CompletedTask;
        }

        internal void CollectData(object state)
        {
            try
            {
                var hangfireStats = _hangfireJobStorage.GetMonitoringApi().GetStatistics();
                var retryJobs = _hangfireJobStorage.GetConnection().GetAllItemsFromSet("retries");
                _metricValues.AddOrUpdate(nameof(hangfireStats.Succeeded), hangfireStats.Succeeded, (_, _) => hangfireStats.Succeeded);
                _metricValues.AddOrUpdate(nameof(hangfireStats.Failed), hangfireStats.Failed, (_, _) => hangfireStats.Failed);
                _metricValues.AddOrUpdate(nameof(hangfireStats.Scheduled), hangfireStats.Scheduled, (_, _) => hangfireStats.Scheduled);
                _metricValues.AddOrUpdate(nameof(hangfireStats.Processing), hangfireStats.Processing, (_, _) => hangfireStats.Processing);
                _metricValues.AddOrUpdate(nameof(hangfireStats.Enqueued), hangfireStats.Enqueued, (_, _) => hangfireStats.Enqueued);
                _metricValues.AddOrUpdate(nameof(hangfireStats.Deleted), hangfireStats.Deleted, (_, _) => hangfireStats.Deleted);
                _metricValues.AddOrUpdate(nameof(hangfireStats.Servers), hangfireStats.Servers, (_, _) => hangfireStats.Servers);
                _metricValues.AddOrUpdate(nameof(hangfireStats.Queues), hangfireStats.Queues, (_, _) => hangfireStats.Queues);
                _metricValues.AddOrUpdate(nameof(hangfireStats.Recurring), hangfireStats.Recurring, (_, _) => hangfireStats.Recurring);
                _metricValues.AddOrUpdate("RetryJobs", retryJobs?.Count ?? 0, (_, _) => retryJobs?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occured while gathering hangfire metrics");
            }
        }

        private static void SetupGauges()
        {
            var metricNames = new[] { "Succeeded", "Failed", "Scheduled", "Processing", "Enqueued", "Deleted", "Servers", "Queues", "Recurring", "RetryJobs" };
            foreach (var metricName in metricNames)
            {
                _metricValues.TryAdd(metricName, 0);
                Metrics.CreateObservableGauge($"{MetricName}_{metricName}", () => ProvideMetric(metricName), "calls", "description");
            }
        }

        private static long ProvideMetric(string name)
        {
            _metricValues.TryGetValue(name, out var value);
            return value;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            return Task.CompletedTask;
        }
    }
}

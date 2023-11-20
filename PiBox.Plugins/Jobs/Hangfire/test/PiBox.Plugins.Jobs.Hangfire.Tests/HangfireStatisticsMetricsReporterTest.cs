using FluentAssertions;
using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Metrics;
using PiBox.Testing;
using PiBox.Testing.Assertions;

namespace PiBox.Plugins.Jobs.Hangfire.Tests
{
    [TestFixture, FixtureLifeCycle(LifeCycle.SingleInstance)]
    public class HangfireStatisticsMetricsReporterTest
    {
        [SetUp]
        public void Setup()
        {
            Metrics.OverrideMeter(new(Guid.NewGuid().ToString("N"), "0.0.0"));
        }

        [Test]
        public void CollectDataSetsLogsException()
        {
            var logger = new FakeLogger<HangfireStatisticsMetricsReporter>();
            var hangfireJobStorage = Substitute.For<JobStorage>();
            using var reporter = new HangfireStatisticsMetricsReporter(hangfireJobStorage, logger);
            reporter.CollectData(null!);
            logger.Entries.Should().HaveCount(1);
            logger.Entries[0].Message.Should().Be("Exception occured while gathering hangfire metrics");
            logger.Entries[0].Level.Should().Be(LogLevel.Error);
            logger.Entries[0].Exception.Should().BeOfType<NullReferenceException>();
        }

        [Test]
        public void DisposeShouldWorkWithNullTimerObject()
        {
            var logger = new FakeLogger<HangfireStatisticsMetricsReporter>();
            var hangfireJobStorage = Substitute.For<JobStorage>();
            var reporter = new HangfireStatisticsMetricsReporter(hangfireJobStorage, logger);

            reporter.Invoking(x => x.Dispose()).Should().NotThrow();
        }

        [Test]
        public async Task DisposeShouldWorkWithInitializedTimerObject()
        {
            var logger = new FakeLogger<HangfireStatisticsMetricsReporter>();
            var hangfireJobStorage = Substitute.For<JobStorage>();
            var reporter = new HangfireStatisticsMetricsReporter(hangfireJobStorage, logger);

            await reporter.StartAsync(CancellationToken.None);
            reporter.Invoking(x => x.Dispose()).Should().NotThrow();
        }

        [Test]
        public async Task CollectDataSetsTheCurrentGaugeValue()
        {
            using var hangfireJobCountSucceeded = new TestMetricsCollector("hangfire_job_count_Succeeded");
            hangfireJobCountSucceeded.CollectedMetrics.Should().BeEmpty();

            using var hangfireJobCountFailed = new TestMetricsCollector("hangfire_job_count_Failed");
            hangfireJobCountFailed.CollectedMetrics.Should().BeEmpty();

            using var hangfireJobCountScheduled = new TestMetricsCollector("hangfire_job_count_Scheduled");
            hangfireJobCountScheduled.CollectedMetrics.Should().BeEmpty();

            using var hangfireJobCountProcessing = new TestMetricsCollector("hangfire_job_count_Processing");
            hangfireJobCountProcessing.CollectedMetrics.Should().BeEmpty();

            using var hangfireJobCountEnqueued = new TestMetricsCollector("hangfire_job_count_Enqueued");
            hangfireJobCountEnqueued.CollectedMetrics.Should().BeEmpty();

            using var hangfireJobCountDeleted = new TestMetricsCollector("hangfire_job_count_Deleted");
            hangfireJobCountDeleted.CollectedMetrics.Should().BeEmpty();

            using var hangfireJobCountServers = new TestMetricsCollector("hangfire_job_count_Servers");
            hangfireJobCountServers.CollectedMetrics.Should().BeEmpty();

            using var hangfireJobCountQueues = new TestMetricsCollector("hangfire_job_count_Queues");
            hangfireJobCountQueues.CollectedMetrics.Should().BeEmpty();

            using var hangfireJobCountRecurring = new TestMetricsCollector("hangfire_job_count_Recurring");
            hangfireJobCountRecurring.CollectedMetrics.Should().BeEmpty();

            using var hangfireJobCountRetryJobs = new TestMetricsCollector("hangfire_job_count_RetryJobs");
            hangfireJobCountRetryJobs.CollectedMetrics.Should().BeEmpty();

            var hangfireJobStorage = Substitute.For<JobStorage>();
            var monitoringApi = Substitute.For<IMonitoringApi>();
            hangfireJobStorage.GetMonitoringApi().Returns(monitoringApi);
            monitoringApi.GetStatistics().Returns(new StatisticsDto
            {
                Succeeded = 1,
                Deleted = 1,
                Enqueued = 1,
                Failed = 1,
                Processing = 1,
                Queues = 1,
                Recurring = 1,
                Scheduled = 1,
                Servers = 1
            });
            hangfireJobStorage.GetConnection().GetAllItemsFromSet("retries").Returns(new HashSet<string> { "retries" });

            using (var reporter = new HangfireStatisticsMetricsReporter(hangfireJobStorage,
                       Substitute.For<ILogger<HangfireStatisticsMetricsReporter>>()))
            {
                await reporter.StartAsync(CancellationToken.None);
                reporter.CollectData(null!);
                await reporter.StopAsync(CancellationToken.None);
            }

            hangfireJobCountSucceeded.RecordObservableInstruments();
            hangfireJobCountSucceeded.Instruments.Should().Contain("hangfire_job_count_Succeeded");
            hangfireJobCountSucceeded.CollectedMetrics.Should().ContainsMetric(1);

            hangfireJobCountFailed.RecordObservableInstruments();
            hangfireJobCountFailed.Instruments.Should().Contain("hangfire_job_count_Failed");
            hangfireJobCountFailed.CollectedMetrics.Should().ContainsMetric(1);

            hangfireJobCountScheduled.RecordObservableInstruments();
            hangfireJobCountScheduled.Instruments.Should().Contain("hangfire_job_count_Scheduled");
            hangfireJobCountScheduled.CollectedMetrics.Should().ContainsMetric(1);

            hangfireJobCountProcessing.RecordObservableInstruments();
            hangfireJobCountProcessing.Instruments.Should().Contain("hangfire_job_count_Processing");
            hangfireJobCountProcessing.CollectedMetrics.Should().ContainsMetric(1);

            hangfireJobCountEnqueued.RecordObservableInstruments();
            hangfireJobCountEnqueued.Instruments.Should().Contain("hangfire_job_count_Enqueued");
            hangfireJobCountEnqueued.CollectedMetrics.Should().ContainsMetric(1);

            hangfireJobCountDeleted.RecordObservableInstruments();
            hangfireJobCountDeleted.Instruments.Should().Contain("hangfire_job_count_Deleted");
            hangfireJobCountDeleted.CollectedMetrics.Should().ContainsMetric(1);

            hangfireJobCountServers.RecordObservableInstruments();
            hangfireJobCountServers.Instruments.Should().Contain("hangfire_job_count_Servers");
            hangfireJobCountServers.CollectedMetrics.Should().ContainsMetric(1);

            hangfireJobCountQueues.RecordObservableInstruments();
            hangfireJobCountQueues.Instruments.Should().Contain("hangfire_job_count_Queues");
            hangfireJobCountQueues.CollectedMetrics.Should().ContainsMetric(1);

            hangfireJobCountRecurring.RecordObservableInstruments();
            hangfireJobCountRecurring.Instruments.Should().Contain("hangfire_job_count_Recurring");
            hangfireJobCountRecurring.CollectedMetrics.Should().ContainsMetric(1);

            hangfireJobCountRetryJobs.RecordObservableInstruments();
            hangfireJobCountRetryJobs.Instruments.Should().Contain("hangfire_job_count_RetryJobs");
            hangfireJobCountRetryJobs.CollectedMetrics.Should().ContainsMetric(1);
        }
    }
}

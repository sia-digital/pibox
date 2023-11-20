using FluentAssertions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Metrics;
using PiBox.Testing;

namespace PiBox.Plugins.Persistence.EntityFramework.Tests
{
    [TestFixture]
    public class ObserverTests
    {
        [Test]
        [TestCase("Microsoft.EntityFrameworkCore.Infrastructure.ContextInitialized", "efcore_dbcontext_created_total")]
        [TestCase("Microsoft.EntityFrameworkCore.Infrastructure.ContextInitialized", "efcore_dbcontext_created_total")]
        [TestCase("Microsoft.EntityFrameworkCore.Infrastructure.ContextDisposed", "efcore_dbcontext_disposed_total")]
        [TestCase("Microsoft.EntityFrameworkCore.Database.Connection.ConnectionOpened",
            "efcore_connection_opened_total")]
        [TestCase("Microsoft.EntityFrameworkCore.Database.Connection.ConnectionClosed",
            "efcore_connection_closed_total")]
        [TestCase("Microsoft.EntityFrameworkCore.Database.Connection.ConnectionError",
            "efcore_connection_errors_total")]
        public void IncrementMetricWorks(string key, string expectedCounterName)
        {

            using var metricsCollector = new TestMetricsCollector(expectedCounterName);
            metricsCollector.CollectedMetrics.Should().BeEmpty();
            var observer = new MetricsObserver();
            observer.OnCompleted();
            observer.OnError(null!);

            var keyValuePair = new KeyValuePair<string, object>(key, string.Empty);
            observer.OnNext(keyValuePair);

            metricsCollector.CollectedMetrics.Should().ContainsMetric(1);
            metricsCollector.Instruments.Should().Contain(expectedCounterName);
        }

        [TestCase("Microsoft.EntityFrameworkCore.Database.Command.CommandError", "efcore_command_errors_total",
            "command")]
        [TestCase("Microsoft.EntityFrameworkCore.Query.QueryPossibleUnintendedUseOfEqualsWarning",
            "efcore_query_warnings_total", "QueryPossibleUnintendedUseOfEqualsWarning")]
        [TestCase("Microsoft.EntityFrameworkCore.Query.QueryPossibleExceptionWithAggregateOperatorWarning",
            "efcore_query_warnings_total", "QueryPossibleExceptionWithAggregateOperatorWarning")]
        [TestCase("Microsoft.EntityFrameworkCore.Query.ModelValidationKeyDefaultValueWarning",
            "efcore_query_warnings_total", "ModelValidationKeyDefaultValueWarning")]
        [TestCase("Microsoft.EntityFrameworkCore.Query.BoolWithDefaultWarning", "efcore_query_warnings_total",
            "BoolWithDefaultWarning")]
        public void IncrementMetricWithLabelTagWorks(string key, string expectedCounterName, string expectedLabelTag)
        {
            using var metricsCollector = new TestMetricsCollector(expectedCounterName);
            metricsCollector.CollectedMetrics.Should().BeEmpty();
            var observer = new MetricsObserver();
            observer.OnCompleted();
            observer.OnError(null!);

            var keyValuePair = new KeyValuePair<string, object>(key, string.Empty);
            observer.OnNext(keyValuePair);
            metricsCollector.CollectedMetrics.Should().ContainsMetric(1, "label", expectedLabelTag);
            metricsCollector.Instruments.Should().Contain(expectedCounterName);

        }

        [TestCase("Microsoft.EntityFrameworkCore.Database.Transaction.TransactionCommitted",
            "efcore_transaction_committed_total")]
        [TestCase("Microsoft.EntityFrameworkCore.Database.Transaction.TransactionRolledBack",
            "efcore_transaction_rollback_total")]
        public void IncrementMetricWithCustomEventTypeWorks(string key, string expectedCounterName)
        {
            using var metricsCollector = new TestMetricsCollector(expectedCounterName);
            metricsCollector.CollectedMetrics.Should().BeEmpty();
            var observer = new MetricsObserver();
            observer.OnCompleted();
            observer.OnError(null!);

            var keyValuePair =
                new KeyValuePair<string, object>(key,
                    new TransactionErrorEventData(null!, null!, null!, null!, Guid.Empty, Guid.Empty, false, null!,
                        null!,
                        DateTimeOffset.UtcNow, TimeSpan.Zero));
            observer.OnNext(keyValuePair);
            metricsCollector.CollectedMetrics.Should().ContainsMetric(1);
            metricsCollector.Instruments.Should().Contain(expectedCounterName);
        }

        [TestCase("Microsoft.EntityFrameworkCore.Database.Transaction.TransactionError", "efcore_command_errors_total",
            "transaction")]
        public void IncrementMetricWithCustomEventTypeAndLabelTagWorks(string key, string expectedCounterName,
            string expectedLabelTag)
        {
            using var metricsCollector = new TestMetricsCollector(expectedCounterName);
            metricsCollector.CollectedMetrics.Should().BeEmpty();
            var observer = new MetricsObserver();
            observer.OnCompleted();
            observer.OnError(null!);

            var keyValuePair =
                new KeyValuePair<string, object>(key,
                    new TransactionErrorEventData(null!, null!, null!, null, Guid.Empty, Guid.Empty, false, null!,
                        null!,
                        DateTimeOffset.UtcNow, TimeSpan.Zero));
            observer.OnNext(keyValuePair);
            metricsCollector.CollectedMetrics.Should().ContainsMetric(1, "label", expectedLabelTag);
            metricsCollector.Instruments.Should().Contain(expectedCounterName);
        }

        [Test]
        public void UpdateHistogramWorks()
        {
            string expectedCounterName = "efcore_command_duration_seconds";
            using var metricsCollector = new TestMetricsCollector(expectedCounterName);
            metricsCollector.CollectedMetrics.Should().BeEmpty();
            var observer = new MetricsObserver();
            observer.OnCompleted();
            observer.OnError(null!);

            var keyValuePair =
                new KeyValuePair<string, object>("Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted",
                    new CommandExecutedEventData(null!, null!, null!, null!, null!, DbCommandMethod.ExecuteNonQuery,
                        Guid.Empty, Guid.Empty, null!, false, false, DateTimeOffset.Now, TimeSpan.FromSeconds(10),
                        CommandSource.Unknown));
            observer.OnNext(keyValuePair);
            metricsCollector.CollectedMetrics.Should().ContainsMetric(10);
            metricsCollector.Instruments.Should().Contain(expectedCounterName);
        }

        [Test]
        public void MetricsAreOnlyCreatedOnce()
        {
            var counter = Metrics.CreateCounter<long>("test", "calls", "some test calls");
            var counter2 = Metrics.CreateCounter<long>("test", "calls", "some test calls");
            var counter3 = Metrics.CreateCounter<long>("test1", "calls", "some test calls");
            counter.Should().Be(counter2);
            counter.Should().NotBe(counter3);
        }
    }
}

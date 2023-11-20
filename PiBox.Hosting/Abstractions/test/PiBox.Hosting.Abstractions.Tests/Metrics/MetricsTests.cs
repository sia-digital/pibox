using System.Diagnostics.Metrics;
using FluentAssertions;
using NUnit.Framework;
using PiBox.Testing;
using Metric = PiBox.Hosting.Abstractions.Metrics.Metrics;

namespace PiBox.Hosting.Abstractions.Tests.Metrics
{
    public class MetricsTests
    {
        [Test]
        public void CreateCounterContainsMetric()
        {
            Metric.OverrideMeter(new("unit-test", "0.0.0"));
            var counter = Metric.CreateCounter<long>("test", "calls", "desc");

            var collector = new TestMetricsCollector("test");
            collector.CollectedMetrics.Should().BeEmpty();

            counter.Add(1);
            collector.Instruments.Should().Contain("test");
            collector.CollectedMetrics.Should().ContainsMetric(1);
        }

        [Test]
        public void CreateHistogramContainsMetric()
        {
            Metric.OverrideMeter(new("unit-test", "0.0.0"));
            var counter = Metric.CreateHistogram<long>("test", "calls", "desc");

            var collector = new TestMetricsCollector("test");
            collector.CollectedMetrics.Should().BeEmpty();

            counter.Record(1);
            collector.Instruments.Should().Contain("test");
            collector.CollectedMetrics.Should().ContainsMetric(1);
        }

        [Test]
        public void CreateObservableCounterContainsMetric()
        {
            Metric.OverrideMeter(new("unit-test", "0.0.0"));
            var observableValue = 0L;
            Metric.CreateObservableCounter("test",
                // ReSharper disable once AccessToModifiedClosure
                () => observableValue, "calls", "desc");

            var collector = new TestMetricsCollector("test");
            collector.CollectedMetrics.Should().BeEmpty();

            observableValue++;
            collector.RecordObservableInstruments();
            collector.Instruments.Should().Contain("test");
            collector.CollectedMetrics.Should().ContainsMetric(1);
        }

        [Test]
        public void CreateObservableCounterMeasurementContainsMetric()
        {
            Metric.OverrideMeter(new("unit-test", "0.0.0"));
            var observableValue = new Measurement<long>();
            Metric.CreateObservableCounter("test",
                // ReSharper disable once AccessToModifiedClosure
                () => observableValue, "calls", "desc");

            var collector = new TestMetricsCollector("test");
            collector.CollectedMetrics.Should().BeEmpty();

            observableValue = new Measurement<long>(1);
            collector.RecordObservableInstruments();
            collector.Instruments.Should().Contain("test");
            collector.CollectedMetrics.Should().ContainsMetric(1);
        }

        [Test]
        public void CreateObservableCounterListMeasurementContainsMetric()
        {
            Metric.OverrideMeter(new("unit-test", "0.0.0"));
            var observableValue = new List<Measurement<long>>();
            Metric.CreateObservableCounter("test",
                () => observableValue, "calls", "desc");

            var collector = new TestMetricsCollector("test");
            collector.CollectedMetrics.Should().BeEmpty();

            observableValue.Add(new Measurement<long>(1));
            collector.RecordObservableInstruments();
            collector.Instruments.Should().Contain("test");
            collector.CollectedMetrics.Should().ContainsMetric(1);
        }

        [Test]
        public void CreateObservableGaugeContainsMetric()
        {
            Metric.OverrideMeter(new("unit-test", "0.0.0"));
            var observableValue = 0L;
            Metric.CreateObservableGauge("test",
                // ReSharper disable once AccessToModifiedClosure
                () => observableValue, "calls", "desc");

            var collector = new TestMetricsCollector("test");
            collector.CollectedMetrics.Should().BeEmpty();

            observableValue++;
            collector.RecordObservableInstruments();
            collector.Instruments.Should().Contain("test");
            collector.CollectedMetrics.Should().ContainsMetric(1);
        }

        [Test]
        public void CreateObservableGaugeMeasurementContainsMetric()
        {
            Metric.OverrideMeter(new("unit-test", "0.0.0"));
            var observableValue = new Measurement<long>();
            Metric.CreateObservableGauge("test",
                // ReSharper disable once AccessToModifiedClosure
                () => observableValue, "calls", "desc");

            var collector = new TestMetricsCollector("test");
            collector.CollectedMetrics.Should().BeEmpty();

            observableValue = new Measurement<long>(1);
            collector.RecordObservableInstruments();
            collector.Instruments.Should().Contain("test");
            collector.CollectedMetrics.Should().ContainsMetric(1);
        }

        [Test]
        public void CreateObservableGaugeListMeasurementContainsMetric()
        {
            Metric.OverrideMeter(new("unit-test", "0.0.0"));
            var observableValue = new List<Measurement<long>>();
            Metric.CreateObservableGauge("test",
                () => observableValue, "calls", "desc");

            var collector = new TestMetricsCollector("test");
            collector.CollectedMetrics.Should().BeEmpty();

            observableValue.Add(new(1));
            collector.RecordObservableInstruments();
            collector.Instruments.Should().Contain("test");
            collector.CollectedMetrics.Should().ContainsMetric(1);
        }

        [Test]
        public void MetricsAreOnlyCreatedOnce()
        {
            Metric.OverrideMeter(new("unit-test", "0.0.0"));
            var counter = Metric.CreateCounter<long>("test", "calls", "some test calls");
            var counter2 = Metric.CreateCounter<long>("test", "calls", "some test calls");
            var counter3 = Metric.CreateCounter<long>("test1", "calls", "some test calls");
            counter.Should().Be(counter2);
            counter.Should().NotBe(counter3);

            var testMetricsCollector = new TestMetricsCollector("test");
            testMetricsCollector.CollectedMetrics.Should().BeEmpty();
            var test1MetricsCollector = new TestMetricsCollector("test1");
            test1MetricsCollector.CollectedMetrics.Should().BeEmpty();

            counter.Add(1);
            testMetricsCollector.CollectedMetrics.Should().ContainsMetric(1);
            counter2.Add(5);
            testMetricsCollector.CollectedMetrics.Should().ContainsMetric(5);
            counter3.Add(3);
            test1MetricsCollector.CollectedMetrics.Should().ContainsMetric(3);

            testMetricsCollector.Instruments.Should().Contain("test");
            test1MetricsCollector.Instruments.Should().Contain("test1");
        }
    }
}

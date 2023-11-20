using System.Diagnostics.Metrics;
using FluentAssertions;
using FluentAssertions.Collections;
using FluentAssertions.Execution;

namespace PiBox.Testing
{
    public record MetricMeasurement(long Measurement, IEnumerable<KeyValuePair<string, string>> Tags);

    public sealed class TestMetricsCollector : IDisposable
    {
        private readonly MeterListener _myMeterListener;
        public List<MetricMeasurement> CollectedMetrics { get; } = new();

        public long GetSum(KeyValuePair<string, string>[] tag = null)
        {
            if (tag == null)
            {
                return CollectedMetrics.Sum(x => x.Measurement);
            }
            return CollectedMetrics.Where(x => x.Tags.SequenceEqual(tag))
                .Select(x => x.Measurement).Sum();
        }

        public void RecordObservableInstruments()
        {
            _myMeterListener.RecordObservableInstruments();
        }

        public List<string> Instruments { get; } = new();

        public TestMetricsCollector(string metricName)
        {
            _myMeterListener = new MeterListener();
            _myMeterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Name == metricName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }

                Instruments.Add(instrument.Name);
            };
            _myMeterListener.SetMeasurementEventCallback<long>(OnMeasurementWritten);
            _myMeterListener.Start();
        }

        private void OnMeasurementWritten(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object>> tags, object state)
        {
            CollectedMetrics.Add(new(measurement, tags.ToArray().Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString()))));
        }

        public void Dispose()
        {
            _myMeterListener?.Dispose();
        }
    }

    public static class TestMetricsCollectorExtensions
    {
        public static TestMetricsCollectorAssertions Should(this List<MetricMeasurement> instance)
        {
            return new(instance);
        }
    }

    public class TestMetricsCollectorAssertions :
        GenericCollectionAssertions<MetricMeasurement>

    {
        public TestMetricsCollectorAssertions(List<MetricMeasurement> instance)
            : base(instance)
        {
        }

        protected override string Identifier => "collection";
        // ReSharper disable once UnusedMethodReturnValue.Global
        public AndConstraint<TestMetricsCollectorAssertions> ContainsMetric(
            long value, string expectedTagKey, string expectedTagValue, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .Given(() => Subject)
                .ForCondition(c => c.Any(tuple => tuple.Measurement == value && tuple.Tags
                    .Any(x => x.Key == expectedTagKey && x.Value == expectedTagValue)))
                .FailWith("Expected {context:measurements} to contain item with value {0} tagkey {1} tagValue {2}{reason}, but found {3}.",
                    _ => value, _ => expectedTagKey, _ => expectedTagValue, measurements => System.Text.Json.JsonSerializer.Serialize(measurements));

            return new AndConstraint<TestMetricsCollectorAssertions>(this);
        }

        public AndConstraint<TestMetricsCollectorAssertions> ContainsMetric(
            long value, KeyValuePair<string, string>[] tags, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .Given(() => Subject)
                .ForCondition(c => c.Any(tuple => tuple.Measurement == value && tuple.Tags
                    .SequenceEqual(tags)))
                .FailWith("Expected {context:measurements} to contain item with value {0} tags {1}{reason}, but found {2}.",
                    _ => value, tags => System.Text.Json.JsonSerializer.Serialize(tags), measurements => System.Text.Json.JsonSerializer.Serialize(measurements));

            return new AndConstraint<TestMetricsCollectorAssertions>(this);
        }

        public AndConstraint<TestMetricsCollectorAssertions> ContainsMetric(
            long value, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .Given(() => Subject)
                .ForCondition(c => c.Any(tuple => tuple.Measurement == value))
                .FailWith("Expected {context:measurements} to contain item with value {0}{reason}, but found {1}.",
                    _ => value, measurements => System.Text.Json.JsonSerializer.Serialize(measurements));

            return new AndConstraint<TestMetricsCollectorAssertions>(this);
        }
        // ReSharper enable once UnusedMethodReturnValue.Global
    }
}

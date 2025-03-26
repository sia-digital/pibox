using System.Diagnostics.Metrics;

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
}

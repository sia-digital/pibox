using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace PiBox.Hosting.Abstractions.Metrics
{
    public static class Metrics
    {
        private static readonly ConcurrentDictionary<string, object> _meters = new();
        private const string CounterPrefix = "counter_";
        private const string HistogramPrefix = "histogram_";
        private const string ObsGaugePrefix = "obs_gauge_";
        private const string ObsCounterPrefix = "obs_counter_";

        private static T GetOrAdd<T>(string name, Func<T> creator)
        {
            if (_meters.TryGetValue(name, out var value))
                return (T)value;
            var instance = creator();
            _meters.TryAdd(name, instance);
            return instance;
        }

        private static Meter Meter { get; set; } = new(Assembly.GetEntryAssembly()?.GetName().Name ?? "unknown", Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0");

        public static void OverrideMeter(Meter meter) => Meter = meter;

        public static Counter<T> CreateCounter<T>(string name, string unit, string description) where T : struct
        {
            return GetOrAdd(CounterPrefix + name, () => Meter.CreateCounter<T>(name, unit, description));
        }

        public static Histogram<T> CreateHistogram<T>(string name, string unit, string description) where T : struct
        {
            return GetOrAdd(HistogramPrefix + name, () => Meter.CreateHistogram<T>(name, unit, description));
        }

        public static ObservableGauge<T> CreateObservableGauge<T>(string name, Func<T> observeValue, string unit, string description) where T : struct
        {
            return GetOrAdd(ObsGaugePrefix + name, () => Meter.CreateObservableGauge(name, observeValue, unit, description));
        }

        public static ObservableGauge<T> CreateObservableGauge<T>(string name, Func<Measurement<T>> observeValue, string unit, string description) where T : struct
        {
            return GetOrAdd(ObsGaugePrefix + name, () => Meter.CreateObservableGauge(name, observeValue, unit, description));
        }

        public static ObservableGauge<T> CreateObservableGauge<T>(string name, Func<IEnumerable<Measurement<T>>> observeValues, string unit, string description) where T : struct
        {
            return GetOrAdd(ObsGaugePrefix + name, () => Meter.CreateObservableGauge(name, observeValues, unit, description));
        }

        public static ObservableCounter<T> CreateObservableCounter<T>(string name, Func<T> observeValue, string unit, string description) where T : struct
        {
            return GetOrAdd(ObsCounterPrefix + name, () => Meter.CreateObservableCounter(name, observeValue, unit, description));
        }

        public static ObservableCounter<T> CreateObservableCounter<T>(string name, Func<Measurement<T>> observeValue, string unit, string description) where T : struct
        {
            return GetOrAdd(ObsCounterPrefix + name, () => Meter.CreateObservableCounter(name, observeValue, unit, description));
        }

        public static ObservableCounter<T> CreateObservableCounter<T>(string name, Func<IEnumerable<Measurement<T>>> observeValues, string unit, string description)
            where T : struct
        {
            return GetOrAdd(ObsCounterPrefix + name, () => Meter.CreateObservableCounter(name, observeValues, unit, description));
        }
    }
}

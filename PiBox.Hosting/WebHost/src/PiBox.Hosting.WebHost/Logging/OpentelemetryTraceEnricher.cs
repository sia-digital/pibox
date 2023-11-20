using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace PiBox.Hosting.WebHost.Logging
{
    internal class OpentelemetryTraceEnricher : ILogEventEnricher
    {
        private const string SpanIdKey = "Serilog.SpanId";
        private const string TraceIdKey = "Serilog.TraceId";

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            ArgumentNullException.ThrowIfNull(logEvent);
            var activity = Activity.Current;

            if (activity is null) return;
            Add(logEvent, activity, SpanIdKey, new("SpanId", new ScalarValue(GetFormatAgnostic(activity, activity.Id!, activity.SpanId.ToHexString()))));
            Add(logEvent, activity, TraceIdKey, new("TraceId", new ScalarValue(GetFormatAgnostic(activity, activity.RootId!, activity.TraceId.ToHexString()))));
        }

        private static void Add(LogEvent logEvent, Activity activity, string idKey, LogEventProperty newLogEventProperty)
        {
            var property = activity.GetCustomProperty(idKey);
            if (property is not LogEventProperty logEventProperty)
            {
                logEventProperty = newLogEventProperty;
                activity.SetCustomProperty(idKey, logEventProperty);
            }

            logEvent.AddPropertyIfAbsent(logEventProperty);
        }

        private static string GetFormatAgnostic(Activity activity, string hierarchicalSelector, string w3cSelector)
        {
            ArgumentNullException.ThrowIfNull(activity);

            var id = activity.IdFormat switch
            {
                ActivityIdFormat.Hierarchical => hierarchicalSelector,
                ActivityIdFormat.W3C => w3cSelector,
                ActivityIdFormat.Unknown => null,
                _ => null
            };

            return id ?? string.Empty;
        }
    }
}

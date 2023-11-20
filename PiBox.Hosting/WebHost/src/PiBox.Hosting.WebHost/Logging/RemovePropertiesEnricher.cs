using Serilog.Core;
using Serilog.Events;

namespace PiBox.Hosting.WebHost.Logging
{
    internal class RemovePropertiesEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            // should not be needed, and if then we got bigger problems
            logEvent.RemovePropertyIfPresent("ConnectionId");
            logEvent.RemovePropertyIfPresent("thread_id");
            logEvent.RemovePropertyIfPresent("thread_name");
        }
    }
}

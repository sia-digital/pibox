using FluentAssertions;
using NUnit.Framework;
using PiBox.Hosting.WebHost.Logging;
using Serilog.Events;

namespace PiBox.Hosting.WebHost.Tests.Logging
{
    public class RemovePropertiesEnricherTests
    {
        [Test]
        public void RemovePropertiesShouldWork()
        {
            var logEvent = new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, MessageTemplate.Empty,
                new List<LogEventProperty>()
                {
                    new("ConnectionId", new ScalarValue("ConnectionId")), new("thread_Name", new ScalarValue("thread_name")), new("thread_id", new ScalarValue("thread_id")),
                });
            var enricher = new RemovePropertiesEnricher();
            enricher.Enrich(logEvent, null);
            logEvent.Properties.Should().NotContainKey("ConnectionId");
            logEvent.Properties.Should().NotContainKey("thread_name");
            logEvent.Properties.Should().NotContainKey("thread_id");
        }
    }
}

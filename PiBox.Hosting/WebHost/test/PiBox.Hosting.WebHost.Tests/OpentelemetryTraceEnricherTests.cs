using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;
using PiBox.Hosting.WebHost.Logging;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace PiBox.Hosting.WebHost.Tests
{
    public class OpentelemetryTraceEnricherTests
    {
        private static readonly ActivitySource _source = new("OpentelemetryTraceEnricherTests", "1.0.0");

        private static readonly ActivityListener _listener = new()
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };

        private readonly ILogEventEnricher _openTelemetryTraceEnricher = new OpentelemetryTraceEnricher();

        [SetUp]
        public void Init()
        {
            ActivitySource.AddActivityListener(_listener);

        }

        private static LogEvent CreateLogEntry(LogEventLevel level, string message, Exception exception = null) =>
            new(DateTimeOffset.UtcNow, level, exception,
                new MessageTemplate(message, new List<MessageTemplateToken>()), new List<LogEventProperty>());

        [Test]
        [TestCase(ActivityIdFormat.Hierarchical)]
        [TestCase(ActivityIdFormat.W3C)]
        [TestCase(ActivityIdFormat.Unknown)] // weirdly does nothing and falls back to W3C
        [TestCase((ActivityIdFormat)3)]
        public void EnrichLogEntryWithTracePropertiesWhenActivityIsPresent(ActivityIdFormat activityIdFormat)
        {

            using var act = _source.CreateActivity("test", ActivityKind.Internal)!.SetIdFormat(activityIdFormat).Start();
            var logEntry = CreateLogEntry(LogEventLevel.Information, "Test");
            _openTelemetryTraceEnricher.Enrich(logEntry, null);
            logEntry.Properties["TraceId"].Should().NotBe("");
            logEntry.Properties["SpanId"].Should().NotBe("");
        }

        [Test]
        public void NoActivityPresentResultsInNoTraceProperties()
        {
            var logEntry = CreateLogEntry(LogEventLevel.Information, "Test");
            _openTelemetryTraceEnricher.Enrich(logEntry, null);
            logEntry.Properties.Should().NotContainKey("TraceId");
            logEntry.Properties.Should().NotContainKey("SpanId");
        }
    }
}

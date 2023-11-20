using NUnit.Framework;
using PiBox.Hosting.WebHost.Logging;
using Serilog.Events;
using Serilog.Parsing;

namespace PiBox.Hosting.WebHost.Tests
{
    //TODO: make tests useful or remove
    [Explicit("wip currently")]
    public class LoggingAppMetricSinkTests
    {
        private LoggingMetricSink _sink = default!;

        [SetUp]
        public void Init()
        {
            _sink = new LoggingMetricSink();

        }

        private static LogEvent CreateLogEntry(LogEventLevel level, string message, Exception exception = null) =>
            new(DateTimeOffset.UtcNow, level, exception,
                new MessageTemplate(message, new List<MessageTemplateToken>()), new List<LogEventProperty>());

        [Test]
        public void SetsMetricForLogMessage()
        {
            // maybe this could work somehow see https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/test/OpenTelemetry.Tests/Metrics/MetricAPITest.cs for inspriration
            // var exportedItems = new List<Metric>();
            //
            // using var meterProvider = Sdk.CreateMeterProviderBuilder()
            //
            //     .AddMeter(_sink.loggingCounter.Name)
            //     .AddInMemoryExporter(exportedItems)
            //     .Build();
            // meterProvider.ForceFlush(10000);
            var logEntry = CreateLogEntry(LogEventLevel.Information, "Test");
            _sink.Emit(logEntry);
            // exportedItems.Should().HaveCount(1);
            // exportedItems[0].Name.Should().Be("");
        }

        [Test]
        public void SetsMetricForLogMessageWithException()
        {
            var logEntry = CreateLogEntry(LogEventLevel.Error, "Test", new ApplicationException("test"));
            _sink.Emit(logEntry);
        }
    }
}

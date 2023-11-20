using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using PiBox.Hosting.WebHost.Logging;
using Serilog;
using Serilog.Events;

namespace PiBox.Hosting.WebHost.Tests.Logging
{
    [Parallelizable(ParallelScope.None)]
    public class StructuredLoggingTests
    {
        [SetUp]
        public void SetUp()
        {
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", "true");
            StructuredLoggingExtensions.CreateBootstrapLogger(LogEventLevel.Verbose);
        }

        private static UnitTestLogEntry CaptureLog(Action<ILogger> logAction)
        {
            var logger = Log.Logger.ForContext<StructuredLoggingTests>();
            var consoleOut = Console.Out;
            using var sw = new StringWriter();
            Console.SetOut(sw);
            logAction(logger);
            Console.SetOut(consoleOut);
            var logOutput = sw.GetStringBuilder().ToString();
            TestContext.Out.WriteLine(logOutput);
            return JsonConvert.DeserializeObject<UnitTestLogEntry>(logOutput);
        }

        [Test]
        public void CheckDebugLog()
        {
            var logEntry = CaptureLog(logger => logger.Debug("Hallo {Name}", "Test"));
            logEntry.Should().NotBeNull();
            logEntry.HasTimestamp().Should().BeTrue();
            logEntry.GetFullMessage().Should().Be(logEntry.GetShortMessage());
            logEntry.GetFullMessage().Should().Be("Hallo \"Test\"");
            logEntry.GetLevel().Should().Be(7);
            logEntry.GetLevelName().Should().Be("DEBUG");
            logEntry.GetProperty<string>("Name").Should().Be("\"Test\"");
        }

        [Test]
        public void CheckVerboseLog()
        {
            var logEntry = CaptureLog(logger => logger.Verbose("Hallo {Name}", "Test"));
            logEntry.Should().NotBeNull();
            logEntry.HasTimestamp().Should().BeTrue();
            logEntry.GetFullMessage().Should().Be(logEntry.GetShortMessage());
            logEntry.GetFullMessage().Should().Be("Hallo \"Test\"");
            logEntry.GetLevel().Should().Be(7);
            logEntry.GetLevelName().Should().Be("VERBOSE");
            logEntry.GetProperty<string>("Name").Should().Be("\"Test\"");
        }

        [Test]
        public void CheckInfoLog()
        {
            var logEntry = CaptureLog(logger => logger.Information("Hallo {Name}", "Test"));
            logEntry.Should().NotBeNull();
            logEntry.HasTimestamp().Should().BeTrue();
            logEntry.GetFullMessage().Should().Be(logEntry.GetShortMessage());
            logEntry.GetFullMessage().Should().Be("Hallo \"Test\"");
            logEntry.GetLevel().Should().Be(6);
            logEntry.GetLevelName().Should().Be("INFO");
            logEntry.GetProperty<string>("Name").Should().Be("\"Test\"");
        }

        [Test]
        public void CheckWarnLog()
        {
            var logEntry = CaptureLog(logger => logger.Warning("Hallo {Name}", "Test"));
            logEntry.Should().NotBeNull();
            logEntry.HasTimestamp().Should().BeTrue();
            logEntry.GetFullMessage().Should().Be(logEntry.GetShortMessage());
            logEntry.GetFullMessage().Should().Be("Hallo \"Test\"");
            logEntry.GetLevel().Should().Be(4);
            logEntry.GetLevelName().Should().Be("WARN");
            logEntry.GetProperty<string>("Name").Should().Be("\"Test\"");
        }

        [Test]
        public void CheckExceptionLog()
        {
            var customEx = new Exception("custom ex");
            var logEntry = CaptureLog(logger => logger.Error(customEx, "Hallo {Name}", "Test"));
            logEntry.Should().NotBeNull();
            logEntry.HasTimestamp().Should().BeTrue();
            logEntry.GetFullMessage().Should().Be(logEntry.GetShortMessage());
            logEntry.GetFullMessage().Should().Be("Hallo \"Test\"");
            logEntry.GetLevel().Should().Be(3);
            logEntry.GetLevelName().Should().Be("ERROR");
            logEntry.GetProperty<string>("Name").Should().Be("\"Test\"");
            logEntry.GetException().Should().Be(customEx.ToString());
        }

        [Test]
        public void CheckFatalLog()
        {
            var customEx = new Exception("custom ex");
            var logEntry = CaptureLog(logger => logger.Fatal(customEx, "Hallo {Name}", "Test"));
            logEntry.Should().NotBeNull();
            logEntry.HasTimestamp().Should().BeTrue();
            logEntry.GetFullMessage().Should().Be(logEntry.GetShortMessage());
            logEntry.GetFullMessage().Should().Be("Hallo \"Test\"");
            logEntry.GetLevel().Should().Be(2);
            logEntry.GetLevelName().Should().Be("FATAL");
            logEntry.GetProperty<string>("Name").Should().Be("\"Test\"");
            logEntry.GetException().Should().Be(customEx.ToString());
        }

        [Test]
        public void CheckLogWithCustomProperty()
        {
            var logEntry = CaptureLog(logger => logger.Debug("Hallo {Name}, you are {Status}", "Test", "good"));
            logEntry.Should().NotBeNull();
            logEntry.HasTimestamp().Should().BeTrue();
            logEntry.GetFullMessage().Should().Be(logEntry.GetShortMessage());
            logEntry.GetFullMessage().Should().Be("Hallo \"Test\", you are \"good\"");
            logEntry.GetLevel().Should().Be(7);
            logEntry.GetLevelName().Should().Be("DEBUG");
            logEntry.GetProperty<string>("Name").Should().Be("\"Test\"");
            logEntry.GetProperty<string>("Status").Should().Be("\"good\"");
        }

        [Test]
        public void CheckLogWithCustomPropertyWithAtPrefix()
        {
            var parent = new UnitTestPerson() { FirstName = "par", LastName = "ent", Children = new List<UnitTestPerson>() { new UnitTestPerson() { FirstName = "chi", LastName = "ld" } } };

            var logEntry = CaptureLog(logger => logger.Debug("Hallo you are {@UnitTestPerson}", parent));
            logEntry.Should().NotBeNull();
            logEntry.HasTimestamp().Should().BeTrue();
            // short message is max 128 chars long
            logEntry.GetShortMessage().Should().Be(logEntry.GetFullMessage().Substring(0, 128));
            logEntry.GetFullMessage().Should()
                .Be("Hallo you are UnitTestPerson { FirstName: \"par\", LastName: \"ent\", Children: [UnitTestPerson { FirstName: \"chi\", LastName: \"ld\", Children: null }] }");
            logEntry.GetLevel().Should().Be(7);
            logEntry.GetLevelName().Should().Be("DEBUG");
            logEntry.GetProperty<string>("UnitTestPerson").Should()
                .Be("UnitTestPerson { FirstName: \"par\", LastName: \"ent\", Children: [UnitTestPerson { FirstName: \"chi\", LastName: \"ld\", Children: null }] }");
        }
    }
}

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using PiBox.Hosting.WebHost.Logging;
using Serilog.Events;

namespace PiBox.Hosting.WebHost.Tests.Logging
{
    public class RequestLoggingTests
    {
        [Test]
        public void TestDetermineLoggingIsSettingTheLevelToErrorForTheGivenPathsStatuscode500()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/api";
            httpContext.Response.StatusCode = 500;
            var logLevel =
                StructuredLoggingExtensions.DetermineRequestLogLevel(new[] { "/metrics-text" })(httpContext, 0,
                    null);

            LogEventLevel.Error.Should().Be(logLevel);
        }

        [Test]
        public void TestDetermineLoggingIsSettingTheLevelToErrorForTheGivenPathsException()
        {
            var httpContext = new DefaultHttpContext();

            httpContext.Request.Path = "/api";
            httpContext.Response.StatusCode = 200;
            var logLevel =
                StructuredLoggingExtensions.DetermineRequestLogLevel(new[] { "/metrics-text" })(httpContext, 0,
                    new Exception("test"));

            LogEventLevel.Error.Should().Be(logLevel);
        }

        [Test]
        public void TestDetermineLoggingIsExcludingTheGivenPaths()
        {
            var httpContext = new DefaultHttpContext();

            httpContext.Request.Path = "/metrics ";
            httpContext.Response.StatusCode = 200;
            var logLevel =
                StructuredLoggingExtensions.DetermineRequestLogLevel(new[] { "/MeTRICS", "/metrics" })(httpContext,
                    0, null);

            LogEventLevel.Verbose.Should().Be(logLevel);
        }

        [Test]
        public void TestDetermineLoggingIsExcludingTheGivenWildcardPaths()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/metrics-text";
            httpContext.Response.StatusCode = 200;
            var logLevel =
                StructuredLoggingExtensions.DetermineRequestLogLevel(new[] { "/MeTRICS", "/metrics*" })(
                    httpContext, 0, null);

            LogEventLevel.Verbose.Should().Be(logLevel);
        }

        [Test]
        public void TestDetermineLoggingIsNotExcludingTheGivenWildcardPaths()
        {
            var httpContext = new DefaultHttpContext();

            httpContext.Request.Path = "/metrics-text";
            httpContext.Response.StatusCode = 200;
            var logLevel =
                StructuredLoggingExtensions.DetermineRequestLogLevel(new[] { "/hangfire*" })(httpContext, 0, null);

            LogEventLevel.Information.Should().Be(logLevel);
        }

        [TestCase("/metrics-text", new[] { "/MeTRICS", "/metrics" }, 499, null)]
        [TestCase("/metrics ", new[] { "" }, 200, null)]
        public void TestDetermineLoggingIsNotExcludingTheGivenPaths(string actualPath, IEnumerable<string> paths,
            int statuscode, Exception exception)
        {
            var httpContext = new DefaultHttpContext();

            httpContext.Request.Path = actualPath;
            httpContext.Response.StatusCode = statuscode;
            var logLevel =
                StructuredLoggingExtensions.DetermineRequestLogLevel(paths)(httpContext, 0, exception);

            LogEventLevel.Information.Should().Be(logLevel);
        }
    }
}

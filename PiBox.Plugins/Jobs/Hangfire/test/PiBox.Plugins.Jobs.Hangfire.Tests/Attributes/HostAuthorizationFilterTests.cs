using FluentAssertions;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using PiBox.Testing;

namespace PiBox.Plugins.Jobs.Hangfire.Tests.Attributes
{
    public class HostAuthorizationFilterTests
    {
        [Test]
        [TestCase("localhost", "localhost", true)]
        [TestCase("localhost1", "localhost", false)]
        [TestCase("example.com", "localhost", false)]
        [TestCase("example.com", "example.com", true)]
        public void JobIsNotCancelledWhenMatchingFeatureIsEnabled(string actualHost, string allowedHost,
            bool expectedResult)
        {
            var sc = TestingDefaults.ServiceProvider();
            var filter = new HangFirePlugin.HostAuthorizationFilter(allowedHost);
            var result = filter.Authorize(new AspNetCoreDashboardContext(new MemoryStorage(), new DashboardOptions(),
                new DefaultHttpContext() { RequestServices = sc, Request = { Host = new HostString(actualHost) } }));
            result.Should().Be(expectedResult);
        }
    }
}

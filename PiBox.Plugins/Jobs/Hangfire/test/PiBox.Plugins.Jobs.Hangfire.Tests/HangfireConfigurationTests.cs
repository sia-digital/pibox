using FluentAssertions;
using NUnit.Framework;

namespace PiBox.Plugins.Jobs.Hangfire.Tests
{
    public class HangfireConfigurationTests
    {
        [Test]
        public void Config()
        {
            var config = HangfirePluginTests.HangfireConfiguration;

            config.EnableJobsByFeatureManagementConfig.Should().Be(true);
            config.WorkerCount.Should().Be(200);
            config.AllowedDashboardHost.Should().Be("localhost");
            config.PollingIntervalInMs.Should().Be(1000);
        }
    }
}

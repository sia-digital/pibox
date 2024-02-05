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

            config.ConnectionString.Should()
                .Be("Host=testHost;Port=9999;Database=testDatabase;Username=testUser;Password=testPassword;");
            config.Host.Should().Be("testHost");
            config.Port.Should().Be(9999);
            config.Database.Should().Be("testDatabase");
            config.InMemory.Should().Be(true);
            config.EnableJobsByFeatureManagementConfig.Should().Be(true);
            config.WorkerCount.Should().Be(200);
            config.AllowedDashboardHost.Should().Be("localhost");
            config.PollingIntervalInMs.Should().Be(1000);
            config.User.Should().Be("testUser");
            config.Password.Should().Be("testPassword");
        }
    }
}

using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PiBox.Testing;

namespace PiBox.Plugins.Jobs.Hangfire.Tests
{
    public class HangfireExtensionsTests
    {
        [Test]
        public void CanSetupJobsWithAServiceCollection()
        {
            var sc = TestingDefaults.ServiceCollection();
            Action<IJobManager, IServiceProvider> setup = (register, _) =>
                register.RegisterRecurring<TestJobTimeoutAsync>(Cron.Daily());
            sc.ConfigureJobs(setup);
            var sp = sc.BuildServiceProvider();
            var options = sp.GetRequiredService<JobOptions>();
            options.Should().NotBeNull();
            options.ConfigureJobs.Should().Be(setup);
        }
    }
}

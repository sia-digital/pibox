using System.Reflection;
using FluentAssertions;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FeatureManagement;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using NUnit.Framework;
using PiBox.Hosting.Abstractions;
using PiBox.Hosting.Abstractions.Services;
using PiBox.Plugins.Jobs.Hangfire.Attributes;

namespace PiBox.Plugins.Jobs.Hangfire.Tests
{
    public class HangfirePluginTests
    {
        internal static readonly HangfireConfiguration HangfireConfiguration = new()
        {
            AllowedDashboardHost = "localhost",
            PollingIntervalInMs = 1000,
            WorkerCount = 200,
            EnableJobsByFeatureManagementConfig = true,
            Queues = ["default", "test"]
        };

        private IImplementationResolver _implementationResolver;
        private IHangfireConfigurator _configurator;
        private HangFirePlugin GetPlugin() => new(HangfireConfiguration, _implementationResolver, [_configurator]);

        [SetUp]
        public void Up()
        {
            _configurator = Substitute.For<IHangfireConfigurator>();
            _implementationResolver = Substitute.For<IImplementationResolver>();
        }

        [Test]
        public void ConfigureServiceTest()
        {
            var sc = new ServiceCollection();
            sc.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            GetPlugin().ConfigureServices(sc);

            var sp = sc.BuildServiceProvider();

            var hangfireConfiguration = sp.GetRequiredService<IGlobalConfiguration>();
            hangfireConfiguration.Should().NotBeNull();

            var backgroundJobServerOptions = sp.GetServices<IHostedService>();
            backgroundJobServerOptions.Should().NotBeNull();

            var jobManager = sp.GetService<IJobManager>();
            jobManager.Should().NotBeNull();
            jobManager.Should().BeOfType<JobManager>();

            _configurator.Received(1).Configure(Arg.Any<IGlobalConfiguration>());
            _configurator.Received(1).ConfigureServer(Arg.Any<BackgroundJobServerOptions>());
        }

        [Test]
        public void ConfigureApplicationTest()
        {
            _implementationResolver.FindTypes()
                .Returns([typeof(TestJobTimeoutAsync)]);
            JobStorage.Current = new MemoryStorage();
            var sc = new ServiceCollection();
            sc.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

            var plugin = GetPlugin();
            var featureManager = Substitute.For<IFeatureManager>();

            plugin.ConfigureServices(sc);
            sc.AddSingleton(featureManager);
            var serviceProvider = sc.BuildServiceProvider();
            var applicationBuilder = new ApplicationBuilder(serviceProvider);
            plugin.ConfigureApplication(applicationBuilder);

            GlobalJobFilters.Filters.Should().Contain(x => x.Instance.GetType() == typeof(EnabledByFeatureFilter));
            GlobalJobFilters.Filters.Should().Contain(x => x.Instance.GetType() == typeof(LogJobExecutionFilter));
        }

        [Test]
        public void HangfireConfigureHealthChecksWorks()
        {
            var hcBuilder = Substitute.For<IHealthChecksBuilder>();
            var plugin = new HangFirePlugin(HangfireConfiguration, _implementationResolver, []);
            plugin.ConfigureHealthChecks(hcBuilder);
            hcBuilder.Add(Arg.Is<HealthCheckRegistration>(h => h.Name == "hangfire" && h.Tags.Contains(HealthCheckTag.Readiness.Value)))
                .Received(Quantity.Exactly(1));
        }

        [Test]
        public void CanSpecifyServerOptions()
        {
            var sc = new ServiceCollection();
            sc.AddScoped<ILogger<HangfireStatisticsMetricsReporter>, NullLogger<HangfireStatisticsMetricsReporter>>();
            GetPlugin().ConfigureServices(sc);
            var sp = sc.BuildServiceProvider();
            var hangfireHostedService = sp.GetServices<IHostedService>().OfType<BackgroundJobServerHostedService>().First();
            hangfireHostedService.Should().NotBeNull();
            var property = typeof(BackgroundJobServerHostedService).GetField("_options", BindingFlags.NonPublic | BindingFlags.Instance)!;
            property.Should().NotBeNull();
            var options = (property.GetValue(hangfireHostedService) as BackgroundJobServerOptions)!;
            options.Should().NotBeNull();
            options.SchedulePollingInterval.Should().Be(TimeSpan.FromMilliseconds(HangfireConfiguration.PollingIntervalInMs!.Value));
            options.WorkerCount.Should().Be(HangfireConfiguration.WorkerCount!.Value);
        }
    }
}

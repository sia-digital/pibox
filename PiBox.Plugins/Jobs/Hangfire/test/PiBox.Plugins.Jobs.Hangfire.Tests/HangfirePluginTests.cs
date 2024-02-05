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
using PiBox.Plugins.Jobs.Hangfire.Job;

namespace PiBox.Plugins.Jobs.Hangfire.Tests
{
    public class HangfirePluginTests
    {
        internal static HangfireConfiguration HangfireConfiguration = new()
        {
            Database = "testDatabase",
            Host = "testHost",
            Port = 9999,
            Password = "testPassword",
            AllowedDashboardHost = "localhost",
            InMemory = true,
            PollingIntervalInMs = 1000,
            WorkerCount = 200,
            EnableJobsByFeatureManagementConfig = true,
            User = "testUser"
        };

        private readonly IImplementationResolver _implementationResolver = Substitute.For<IImplementationResolver>();

        private HangFirePlugin GetPlugin() => new(HangfireConfiguration, _implementationResolver);

        [Test]
        public void ConfigureServiceTest()
        {
            var sc = new ServiceCollection();
            sc.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            GetPlugin().ConfigureServices(sc);

            var sp = sc.BuildServiceProvider();

            var hangfireConfiguration = sp.GetRequiredService<IGlobalConfiguration>();
            hangfireConfiguration.Should().NotBeNull();
        }

        [Test]
        public void ConfigureApplicationTest()
        {
            _implementationResolver.FindTypes()
                .Returns(new List<Type> { typeof(TestJobAsync) });
            JobStorage.Current = new MemoryStorage();
            var sc = new ServiceCollection();
            sc.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

            var plugin = GetPlugin();
            var featureManager = Substitute.For<IFeatureManager>();

            plugin.ConfigureServices(sc);
            sc.AddSingleton<IFeatureManager>(featureManager);
            var serviceProvider = sc.BuildServiceProvider();
            var applicationBuilder = new ApplicationBuilder(serviceProvider); // need a real application builder here because of UseRouting()
            var jobRegister = serviceProvider.GetRequiredService<IJobRegister>();
            jobRegister.DefaultTimeout = null;
            jobRegister.DefaultTimeZoneInfo = TimeZoneInfo.Utc;
            jobRegister
                .RegisterParameterizedRecurringAsyncJob<ParameterizedAsyncJobTest, string>(Cron.Daily(), "hans");
            jobRegister.RegisterRecurringAsyncJob<JobFailsJob>(Cron.Monthly()).UseTimeout(TimeSpan.FromSeconds(10))
                .UseTimezone(TimeZoneInfo.Local);

            jobRegister
                .RegisterParameterizedRecurringAsyncJob<ParameterizedAsyncJobTest, string>(Cron.Weekly(), "cats",
                    "meow");
            plugin.ConfigureApplication(applicationBuilder);

            var collection = serviceProvider.GetRequiredService<JobDetailCollection>();

            collection[0].Should().BeOfType<JobDetails>();
            collection[0].CronExpression.Should().Be(Cron.Daily());
            collection[0].Name.Should().Be("ParameterizedAsyncJobTest_hans");
            collection[0].Timeout.Should().BeNull();
            collection[0].TimeZoneInfo.Should().Be(TimeZoneInfo.Utc);
            collection[0].JobParameter.Should().Be("hans");
            collection[0].JobType.Should().Be(typeof(ParameterizedAsyncJobTest));

            collection[1].Should().BeOfType<JobDetails>();
            collection[1].CronExpression.Should().Be(Cron.Monthly());
            collection[1].Name.Should().Be("JobFails");
            collection[1].Timeout.Should().Be(TimeSpan.FromSeconds(10));
            collection[1].TimeZoneInfo.Should().Be(TimeZoneInfo.Local);
            collection[1].JobParameter.Should().Be(null);
            collection[1].JobType.Should().Be(typeof(JobFailsJob));

            collection[2].Should().BeOfType<JobDetails>();
            collection[2].CronExpression.Should().Be(Cron.Weekly());
            collection[2].Name.Should().Be("ParameterizedAsyncJobTest_meow");
            collection[2].Timeout.Should().BeNull();
            collection[2].TimeZoneInfo.Should().Be(TimeZoneInfo.Utc);
            collection[2].JobParameter.Should().Be("cats");
            collection[2].JobType.Should().Be(typeof(ParameterizedAsyncJobTest));

            collection[3].Should().BeOfType<JobDetails>();
            collection[3].CronExpression.Should().Be(Cron.Daily());
            collection[3].Name.Should().Be("TestJob");
            collection[3].Timeout.Should().BeNull();
            collection[3].TimeZoneInfo.Should().Be(TimeZoneInfo.Utc);
            collection[3].JobParameter.Should().Be(null);
            collection[3].JobType.Should().Be(typeof(TestJobAsync));

            GlobalJobFilters.Filters.Should().Contain(x => x.Instance.GetType() == typeof(EnabledByFeatureFilter));
        }

        [Test]
        public void HangfireConfigureHealthChecksWorks()
        {
            var hcBuilder = Substitute.For<IHealthChecksBuilder>();
            var plugin = new HangFirePlugin(HangfireConfiguration, _implementationResolver);
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

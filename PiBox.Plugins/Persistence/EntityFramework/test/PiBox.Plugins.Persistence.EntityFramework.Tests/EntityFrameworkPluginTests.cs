using System.Data;
using System.Diagnostics;
using Chronos;
using Chronos.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using NUnit.Framework;
using OpenTelemetry.Instrumentation.EntityFrameworkCore;
using PiBox.Hosting.Abstractions.Services;
using PiBox.Plugins.Persistence.Abstractions;
using PiBox.Testing.Extensions;

namespace PiBox.Plugins.Persistence.EntityFramework.Tests
{
    public class EntityFrameworkPluginTests
    {
        private IImplementationResolver _implementationResolver;
        private EntityFrameworkPlugin _plugin;

        [SetUp]
        public void Up()
        {
            _implementationResolver = Substitute.For<IImplementationResolver>();
            _plugin = new EntityFrameworkPlugin(_implementationResolver);
        }

        [Test]
        public void ConfigureServicesWorks()
        {
            var sc = new ServiceCollection();
            sc.AddSingleton<IDateTimeProvider, DateTimeProvider>();

            // register dbContext and map as interface
            sc.AddDbContext<TestContext>(o => o.UseInMemoryDatabase("test"), ServiceLifetime.Transient);
            sc.AddTransient<IDbContext<TestEntity>, TestContext>();
            _plugin.ConfigureServices(sc);
            var sp = sc.BuildServiceProvider();
            var readRepo = sp.GetRequiredService<IReadRepository<TestEntity>>();
            readRepo.Should().NotBeNull();
            readRepo.Should().BeOfType<EntityFrameworkRepository<TestEntity>>();

            var repo = sp.GetRequiredService<IRepository<TestEntity>>();
            repo.Should().NotBeNull();
            repo.Should().BeOfType<EntityFrameworkRepository<TestEntity>>();
        }

        [Test]
        public void ConfigureApplicationWorks()
        {
            var appBuilder = Substitute.For<IApplicationBuilder>();
            _plugin.ConfigureApplication(appBuilder);

            var subscriptions = DiagnosticListener.AllListeners.GetInaccessibleValue<object>("_subscriptions");
            subscriptions.Should().NotBeNull();
            var subscriber = subscriptions.GetInaccessibleValue<object>("Subscriber");
            subscriber.Should().NotBeNull();
            subscriber.Should().BeOfType<DiagnosticObserver>();
        }

        [Test]
        public void ConfiguresHealthChecks()
        {
            _implementationResolver.FindAssemblies().Returns([typeof(EntityFrameworkPluginTests).Assembly]);
            var healthCheckBuilder = Substitute.For<IHealthChecksBuilder>();
            _plugin.ConfigureHealthChecks(healthCheckBuilder);
            healthCheckBuilder.Received(1).Add(Arg.Any<HealthCheckRegistration>());
            healthCheckBuilder.Received(1).Add(Arg.Is<HealthCheckRegistration>(h => h.Name == nameof(TestContext)));
        }

        [Test]
        public void EnrichEfCoreWithActivitySetsOptions()
        {
            var opts = new EntityFrameworkInstrumentationOptions();
            EntityFrameworkPlugin.EnrichEfCoreWithActivity(opts);
            opts.EnrichWithIDbCommand.Should().NotBeNull();
            using var activity = new Activity("unit-test");
            var command = Substitute.For<IDbCommand>();
            opts.EnrichWithIDbCommand!(activity, command);

            var dbNameTag = activity.Tags.Single(x => x.Key == "db.name");
            dbNameTag.Value.Should().Be(command.CommandType + " main");
            activity.DisplayName.Should().Be(command.CommandType + " main");
        }
    }
}

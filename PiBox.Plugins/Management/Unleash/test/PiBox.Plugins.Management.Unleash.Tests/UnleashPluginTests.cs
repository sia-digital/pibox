using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using NSubstitute;
using NUnit.Framework;
using PiBox.Testing;
using PiBox.Testing.Assertions;
using PiBox.Testing.Extensions;
using Unleash;
using Unleash.Events;
using Unleash.Internal;

namespace PiBox.Plugins.Management.Unleash.Tests
{
    [TestFixture]
    public class UnleashPluginTests
    {
        internal static readonly UnleashConfiguration GetUnleashConfiguration = new()
        {
            AppName = "unittest",
            ApiUri = "http://localhost:4242",
            ApiToken = "my-token",
            ProjectId = "1",
            InstanceTag = "backup-instance",
            Environment = "unittest",
        };

        private UnleashPlugin _plugin = null!;
        private FakeLogger<UnleashPlugin> _fakeLogger;

        [SetUp]
        public void Init()
        {
            _fakeLogger = new FakeLogger<UnleashPlugin>();
            _plugin = new UnleashPlugin(GetUnleashConfiguration, _fakeLogger);
        }

        [Test]
        public void PluginConfiguresServices()
        {
            var sc = TestingDefaults.ServiceCollection();
            _plugin.ConfigureServices(sc);
            sc.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
            var sp = sc.BuildServiceProvider();

            var unleash = sp.GetRequiredService<IUnleash>();
            unleash.Should().NotBeNull();

            var settings = unleash.GetInaccessibleValue<UnleashSettings>("settings");
            settings.Should().NotBeNull();
            settings.AppName.Should().Be(GetUnleashConfiguration.AppName);
            settings.UnleashApi.Should().Be(new Uri(GetUnleashConfiguration.ApiUri));
            settings.CustomHttpHeaders.Should().ContainValue(GetUnleashConfiguration.ApiToken);
            settings.ProjectId.Should().Be(GetUnleashConfiguration.ProjectId);
            settings.InstanceTag.Should().Be(GetUnleashConfiguration.InstanceTag);
            settings.Environment.Should().Be(GetUnleashConfiguration.Environment);

            var blobStorage = sp.GetRequiredService<IFeatureDefinitionProvider>();
            blobStorage.Should().NotBeNull();
            blobStorage.Should().BeOfType<UnleashFeatureDefinitionProvider>();

            var featureFilters = sp.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>();
            featureFilters.Should().Contain(x => x.GetType() == typeof(UnleashFilter));
        }

        [Test]
        public void PluginConfiguresHealthChecks()
        {
            var healthChecksBuilder = Substitute.For<IHealthChecksBuilder>();
            healthChecksBuilder.Services.Returns(new ServiceCollection());
            _plugin.ConfigureHealthChecks(healthChecksBuilder);
            healthChecksBuilder.Received(1)
                .Add(Arg.Is<HealthCheckRegistration>(h => h.Name == "unleash"));
        }

        [Test]
        public void HandleImpressionEventShouldLogDebugAndIncrementMetric()
        {
            using var metricsCollector = new TestMetricsCollector(UnleashPlugin.PiboxUnleashPluginImpressionsTotal);
            metricsCollector.CollectedMetrics.Should().BeEmpty();

            _plugin.HandleImpressionEvent(new ImpressionEvent() { FeatureName = "test", Enabled = true });
            _fakeLogger.Entries.Should().Satisfy(x => x.Message == "ImpressionEvent: test: True" && x.Level == LogLevel.Debug);

            metricsCollector.Instruments.Should().Contain(UnleashPlugin.PiboxUnleashPluginImpressionsTotal);
            metricsCollector.CollectedMetrics.Should().ContainsMetric(1);
        }

        [Test]
        public void HandleErrorEventShouldLogDebugAndIncrementMetric()
        {
            using var metricsCollector = new TestMetricsCollector(UnleashPlugin.PiboxUnleashPluginErrorsTotal);
            metricsCollector.CollectedMetrics.Should().BeEmpty();

            _plugin.HandleErrorEvent(new ErrorEvent() { Error = new Exception("test"), ErrorType = ErrorType.Client });
            _fakeLogger.Entries.Should().Satisfy(x => x.Message == "Unleash System.Exception: test  of type Client occured." && x.Level == LogLevel.Error);

            metricsCollector.Instruments.Should().Contain(UnleashPlugin.PiboxUnleashPluginErrorsTotal);
            metricsCollector.CollectedMetrics.Should().ContainsMetric(1);
        }

        [Test]
        public void HandleToggleUpdatedEventShouldLogDebugAndIncrementMetric()
        {
            using var metricsCollector = new TestMetricsCollector(UnleashPlugin.PiboxUnleashPluginToggleupdatesTotal);
            metricsCollector.CollectedMetrics.Should().BeEmpty();

            _plugin.HandleTogglesUpdatedEvent(new TogglesUpdatedEvent() { UpdatedOn = new DateTime(2023, 11, 16, 1, 1, 1, DateTimeKind.Utc) });
            _fakeLogger.Entries.Should().Satisfy(x => x.Message == "Feature toggles updated on: 11/16/2023 01:01:01" && x.Level == LogLevel.Information);

            metricsCollector.Instruments.Should().Contain(UnleashPlugin.PiboxUnleashPluginToggleupdatesTotal);
            metricsCollector.CollectedMetrics.Should().ContainsMetric(1);
        }
    }
}

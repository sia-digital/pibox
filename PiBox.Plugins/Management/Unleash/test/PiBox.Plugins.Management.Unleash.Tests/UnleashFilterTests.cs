using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.FeatureManagement;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using PiBox.Testing;
using PiBox.Testing.Assertions;
using Unleash;

namespace PiBox.Plugins.Management.Unleash.Tests
{
    [TestFixture]
    public class UnleashFilterTests
    {
        [Test]
        public async Task ExceptionShouldResultInFalse()
        {
            var httpContextAccessor = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };
            var unleash = Substitute.For<IUnleash>();
            unleash.IsEnabled(Arg.Any<string>(), Arg.Any<UnleashContext>()).Throws(new Exception("failure"));
            var filter = new UnleashFilter(new FakeLogger<UnleashFilter>(), unleash,
                httpContextAccessor);

            var result = await filter.EvaluateAsync(new FeatureFilterEvaluationContext() { FeatureName = "test" });

            result.Should().BeFalse();
        }

        [Test]
        public async Task MatchingTogglenameWithAnyContextShouldReturnTrue()
        {
            using var metricsCollector = new TestMetricsCollector(UnleashFilter.PiboxUnleashPluginUnleashFilterCallsTotal);
            metricsCollector.CollectedMetrics.Should().BeEmpty();

            var httpContextAccessor = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };
            var unleash = Substitute.For<IUnleash>();
            unleash.IsEnabled(Arg.Is<string>(x => x == "test"), Arg.Any<UnleashContext>()).Returns(true);
            var filter = new UnleashFilter(new FakeLogger<UnleashFilter>(), unleash,
                httpContextAccessor);

            var result = await filter.EvaluateAsync(new FeatureFilterEvaluationContext() { FeatureName = "test" });

            result.Should().BeTrue();

            metricsCollector.Instruments.Should().Contain(UnleashFilter.PiboxUnleashPluginUnleashFilterCallsTotal);
            metricsCollector.CollectedMetrics.Should().ContainsMetric(1);
        }

        [Test]
        public async Task MatchingTogglenameWithNullContextShouldReturnTrue()
        {
            var httpContextAccessor = new HttpContextAccessor() { };
            var unleash = Substitute.For<IUnleash>();
            unleash.IsEnabled(Arg.Is<string>(x => x == "test"), null).Returns(true);
            var filter = new UnleashFilter(new FakeLogger<UnleashFilter>(), unleash,
                httpContextAccessor);

            var result = await filter.EvaluateAsync(new FeatureFilterEvaluationContext() { FeatureName = "test" });

            result.Should().BeTrue();
        }

        [Test]
        public async Task NonMatchingTogglenameWithAnyContextShouldReturnFalse()
        {
            var httpContextAccessor = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };
            var unleash = Substitute.For<IUnleash>();
            unleash.IsEnabled(Arg.Is<string>(x => x == "test"), Arg.Any<UnleashContext>()).Returns(false);
            var filter = new UnleashFilter(new FakeLogger<UnleashFilter>(), unleash,
                httpContextAccessor);

            var result = await filter.EvaluateAsync(new FeatureFilterEvaluationContext() { FeatureName = "test" });

            result.Should().BeFalse();
        }

        [Test]
        public async Task NonMatchingTogglenameWithNullContextShouldReturnFalse()
        {
            var httpContextAccessor = new HttpContextAccessor() { };
            var unleash = Substitute.For<IUnleash>();
            unleash.IsEnabled(Arg.Is<string>(x => x == "test"), null).Returns(false);
            var filter = new UnleashFilter(new FakeLogger<UnleashFilter>(), unleash,
                httpContextAccessor);

            var result = await filter.EvaluateAsync(new FeatureFilterEvaluationContext() { FeatureName = "test" });

            result.Should().BeFalse();
        }

    }
}

using FluentAssertions;
using Microsoft.FeatureManagement;
using NSubstitute;
using NUnit.Framework;
using Unleash;
using Unleash.Internal;

namespace PiBox.Plugins.Management.Unleash.Tests
{
    [TestFixture]
    public class UnleashFeatureDefinitionProviderTests
    {
        [Test]
        public async Task FeatureNameMisMatchReturnsNull()
        {
            var unleash = Substitute.For<IUnleash>();
            var featureToggles = new List<FeatureToggle>()
            {
                new("test", "release", true, false,
                    new List<ActivationStrategy>() { new("default", new Dictionary<string, string>()) })
            };
            unleash.FeatureToggles.Returns(featureToggles);
            var unleashFeatureDefinitionProvider = new UnleashFeatureDefinitionProvider(unleash);
            var featureDefinition = await unleashFeatureDefinitionProvider.GetFeatureDefinitionAsync("myfancyFeature");

            featureDefinition.Should().BeNull();
        }

        [Test]
        public async Task FeatureWithOneStrategyShouldHaveRequirementTypeAll()
        {
            var unleash = Substitute.For<IUnleash>();
            var featureToggles = new List<FeatureToggle>()
            {
                new("test", "release", true, false,
                    new List<ActivationStrategy>() { new("default", new Dictionary<string, string>()) })
            };
            unleash.FeatureToggles.Returns(featureToggles);
            var unleashFeatureDefinitionProvider = new UnleashFeatureDefinitionProvider(unleash);
            var featureDefinition = await unleashFeatureDefinitionProvider.GetFeatureDefinitionAsync("test");

            featureDefinition.Name.Should().Be(featureToggles[0].Name);
            featureDefinition.EnabledFor.Should().Satisfy(x => x.Name == UnleashFilter.FilterAlias);
            featureDefinition.RequirementType.Should().Be(RequirementType.All);
        }

        [Test]
        public async Task FeatureWithTwoStrategiesShouldHaveRequirementTypeAll()
        {
            var unleash = Substitute.For<IUnleash>();
            var featureToggles = new List<FeatureToggle>()
            {
                new("test", "release", true, false,
                    new List<ActivationStrategy>()
                    {
                        new("default", new Dictionary<string, string>()),
                        new("userid", new Dictionary<string, string>())
                    })
            };
            unleash.FeatureToggles.Returns(featureToggles);
            var unleashFeatureDefinitionProvider = new UnleashFeatureDefinitionProvider(unleash);
            var featureDefinition = await unleashFeatureDefinitionProvider.GetFeatureDefinitionAsync("test");

            featureDefinition.Name.Should().Be(featureToggles[0].Name);
            featureDefinition.EnabledFor.Should().Satisfy(x => x.Name == UnleashFilter.FilterAlias);
            featureDefinition.RequirementType.Should().Be(RequirementType.Any);
        }

        [Test]
        public async Task GetAllFeatureDefinitionsAsyncShouldReturnCorrectFeatureDefinitons()
        {
            var unleash = Substitute.For<IUnleash>();
            var featureToggles = new List<FeatureToggle>()
            {
                new("test", FeatureToggleType.Release, true, false,
                    new List<ActivationStrategy>() { new("default", new Dictionary<string, string>()) }),
                new("another-one", FeatureToggleType.Experiment, true, false,
                    new List<ActivationStrategy>()
                    {
                        new("default", new Dictionary<string, string>()),
                        new("userid", new Dictionary<string, string>())
                    })
            };
            unleash.FeatureToggles.Returns(featureToggles);
            var unleashFeatureDefinitionProvider = new UnleashFeatureDefinitionProvider(unleash);
            var featureDefinition = unleashFeatureDefinitionProvider.GetAllFeatureDefinitionsAsync();

            var enumerator = featureDefinition.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
            enumerator.Current.Name.Should().Be(featureToggles[0].Name);
            enumerator.Current.EnabledFor.Should().Satisfy(x => x.Name == UnleashFilter.FilterAlias);
            enumerator.Current.RequirementType.Should().Be(RequirementType.All);

            await enumerator.MoveNextAsync();
            enumerator.Current.Name.Should().Be(featureToggles[1].Name);
            enumerator.Current.EnabledFor.Should().Satisfy(x => x.Name == UnleashFilter.FilterAlias);
            enumerator.Current.RequirementType.Should().Be(RequirementType.Any);
        }
    }
}

using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using Unleash;
using Unleash.Internal;

namespace PiBox.Plugins.Management.Unleash
{
    public class UnleashFeatureDefinitionProvider : IFeatureDefinitionProvider
    {
        private readonly IUnleash _unleash;

        public UnleashFeatureDefinitionProvider(IUnleash unleash)
        {
            _unleash = unleash;
        }

        public Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
        {
            var feature = _unleash.FeatureToggles.FirstOrDefault(x => x.Name == featureName);
            if (feature == null)
            {
                return Task.FromResult<FeatureDefinition>(null);
            }

            IConfigurationRoot parameters;
            using (var stream = new MemoryStream())
            {
                JsonSerializer.Serialize(stream, feature);
                stream.Position = 0;
                parameters = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();
            }

            var featureDefinition = BuildFeatureDefinition(feature, parameters);

            return Task.FromResult(featureDefinition);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
#pragma warning restore CS1998
        {
            foreach (var toggle in _unleash.FeatureToggles)
            {
                IConfigurationRoot parameters;
                using (var stream = new MemoryStream())
                {
                    JsonSerializer.Serialize(stream, toggle);
                    stream.Position = 0;
                    parameters = new ConfigurationBuilder()
                        .AddJsonStream(stream)
                        .Build();
                }

                yield return BuildFeatureDefinition(toggle, parameters);
            }
        }

        internal static FeatureDefinition BuildFeatureDefinition(FeatureToggle feature, IConfigurationRoot parameters)
        {
            return new FeatureDefinition()
            {
                Name = feature.Name,
                EnabledFor = GetFilterConfigurationList(parameters),
                RequirementType = feature.Strategies.Count > 1 ? RequirementType.Any : RequirementType.All
            };
        }

        internal static List<FeatureFilterConfiguration> GetFilterConfigurationList(IConfigurationRoot parameters)
        {
            return new List<FeatureFilterConfiguration>()
            {
                new() { Name = UnleashFilter.FilterAlias, Parameters = parameters }
            };
        }
    }
}

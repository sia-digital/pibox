using Microsoft.Extensions.Configuration;

namespace PiBox.Hosting.Abstractions.Configuration
{
    public class EnvConfigurationProvider : ConfigurationProvider
    {
        public override void Load()
        {
            var environmentVariables = Environment.GetEnvironmentVariables();
            var enumerator = environmentVariables.GetEnumerator();
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (enumerator.MoveNext())
            {
                var entry = enumerator.Entry;
                var key = (string)entry.Key;
                var normalizedKey = key.Replace("__", "_").Replace("_", ConfigurationPath.KeyDelimiter);
                var value = (string)entry.Value;
                if (!string.IsNullOrEmpty(value))
                    data[normalizedKey] = value;
            }

            Data = data;
            (enumerator as IDisposable)?.Dispose();
        }
    }

    public class EnvConfigurationSource : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new EnvConfigurationProvider();
        }
    }

    public static class EnvConfigurationExtensions
    {
        public static IConfigurationBuilder AddEnvVariables(this IConfigurationBuilder builder) =>
            builder.Add(new EnvConfigurationSource());
    }
}

using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace PiBox.Hosting.Abstractions.Extensions
{
    public static class ConfigurationExtensions
    {
        public static T BindToSection<T>(this IConfiguration configuration, string sectionKey) where T : new()
        {
            var config = new T();
            configuration.Bind(sectionKey, config);
            return config;
        }

        public static object GetSection(this IConfiguration configuration, string sectionKey, Type type)
        {
            var section = configuration.GetSection(sectionKey);
            return ConfigurationSectionToObject(section).Serialize().Deserialize(type);
        }
        private static object ConfigurationSectionToObject(IConfigurationSection section)
        {
            var children = section.GetChildren().ToList();
            if (children.All(child => int.TryParse(child.Key, out _)))
            {
                if (children.All(child => !child.GetChildren().Any()))
                {
                    return children.Select(child => ParseValue(child.Value)).ToList();
                }
                return children.OrderBy(child => int.Parse(child.Key, CultureInfo.InvariantCulture))
                    .Select(ConfigurationSectionToObject)
                    .ToList();
            }
            var result = new Dictionary<string, object>();
            foreach (var child in children)
            {
                result[child.Key] = child.GetChildren().Any() ? ConfigurationSectionToObject(child) : ParseValue(child.Value);
            }
            return result;
        }

        private static object ParseValue(string value)
        {
            if (value is null) return null;

            if (bool.TryParse(value, out var boolValue))
            {
                return boolValue;
            }
            if (int.TryParse(value, out var intValue))
            {
                return intValue;
            }
            if (decimal.TryParse(value, out var decimalValue))
            {
                return decimalValue;
            }
            return value;
        }
    }
}

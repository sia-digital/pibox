using Microsoft.Extensions.Configuration;

namespace PiBox.Testing
{
    public class CustomConfiguration
    {
        private readonly IDictionary<string, string> _configuration;

        public CustomConfiguration(IDictionary<string, string> configuration) =>
            _configuration = configuration;

        public CustomConfiguration Add(string key, string value)
        {
            _configuration.Add(key, value);
            return this;
        }

        public IConfiguration Build() => new ConfigurationBuilder().AddInMemoryCollection(_configuration).Build();
        public static CustomConfiguration Create() => new(new Dictionary<string, string>());
        public static readonly IConfiguration Empty = new CustomConfiguration(new Dictionary<string, string>()).Build();
    }
}

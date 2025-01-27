using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using PiBox.Hosting.Abstractions;
using PiBox.Hosting.Abstractions.Plugins;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Persistence.MongoDb
{
    public class MongoDbPlugin : IPluginServiceConfiguration, IPluginHealthChecksConfiguration
    {
        private readonly MongoDbConfiguration _configuration;

        public MongoDbPlugin(MongoDbConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(_configuration);
            serviceCollection.AddSingleton<IMongoDbInstance, MongoDbInstance>();
            serviceCollection.AddTransient(typeof(IRepository<>), typeof(MongoRepository<>));
            serviceCollection.AddTransient(typeof(IReadRepository<>), typeof(MongoRepository<>));
        }

        public void ConfigureHealthChecks(IHealthChecksBuilder healthChecksBuilder)
        {
            var settings = new MongoClientSettings
            {
                Server = new MongoServerAddress(_configuration.Host, _configuration.Port),
                ConnectTimeout = TimeSpan.FromSeconds(5),
                HeartbeatInterval = TimeSpan.FromSeconds(5)
            };
            if (!string.IsNullOrEmpty(_configuration.User) && !string.IsNullOrEmpty(_configuration.Password))
            {
                settings.Credential = MongoCredential.CreateCredential(_configuration.AuthDatabase ?? _configuration.Database, _configuration.User, _configuration.Password);
            }

            healthChecksBuilder.AddMongoDb(
                _ => new MongoClient(settings),
                _ => _configuration.Database,
                name: "mongo",
                tags: [HealthCheckTag.Readiness.Value]);
        }
    }
}

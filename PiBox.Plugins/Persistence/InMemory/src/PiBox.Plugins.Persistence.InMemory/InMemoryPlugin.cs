using Microsoft.Extensions.DependencyInjection;
using PiBox.Hosting.Abstractions.Plugins;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Persistence.InMemory
{
    public class InMemoryPlugin : IPluginServiceConfiguration
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(typeof(InMemoryStore));
            serviceCollection.AddSingleton(typeof(IRepository<>), typeof(InMemoryRepository<>));
            serviceCollection.AddSingleton(typeof(IReadRepository<>), typeof(InMemoryRepository<>));
        }
    }
}

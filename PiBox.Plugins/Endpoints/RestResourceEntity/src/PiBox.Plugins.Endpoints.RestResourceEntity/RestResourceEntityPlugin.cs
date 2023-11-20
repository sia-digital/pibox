using Microsoft.Extensions.DependencyInjection;
using PiBox.Hosting.Abstractions.Plugins;

namespace PiBox.Plugins.Endpoints.RestResourceEntity
{
    public sealed class RestResourceEntityPlugin : IPluginServiceConfiguration
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<GlobalResponseOptions>();
        }
    }
}

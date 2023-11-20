using Microsoft.AspNetCore.Routing;

namespace PiBox.Hosting.Abstractions.Plugins
{
    public interface IPluginEndpointsConfiguration : IPluginActivateable
    {
        void ConfigureEndpoints(IEndpointRouteBuilder endpointRouteBuilder, IServiceProvider serviceProvider);
    }
}

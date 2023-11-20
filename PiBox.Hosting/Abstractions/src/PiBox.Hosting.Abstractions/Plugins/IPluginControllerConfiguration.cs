using Microsoft.Extensions.DependencyInjection;

namespace PiBox.Hosting.Abstractions.Plugins
{
    public interface IPluginControllerConfiguration : IPluginActivateable
    {
        void ConfigureControllers(IMvcBuilder controllerBuilder);
    }
}

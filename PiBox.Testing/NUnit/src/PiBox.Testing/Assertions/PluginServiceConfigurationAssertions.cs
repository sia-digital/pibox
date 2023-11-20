using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PiBox.Hosting.Abstractions.Plugins;

namespace PiBox.Testing.Assertions
{
    public static class PluginServiceConfigurationAssertions
    {
        public static void ShouldHaveServices(this IPluginServiceConfiguration serviceConfiguration,
            IDictionary<Type, Type> services,
            IServiceCollection serviceCollection = null)
        {
            serviceCollection ??= new ServiceCollection();
            serviceConfiguration.ConfigureServices(serviceCollection);
            var sp = serviceCollection.BuildServiceProvider();
            foreach (var (svc, implementation) in services)
            {
                var instance = sp.GetService(svc);
                instance.Should().NotBeNull($"Expected service ${svc.FullName} not to be null!");
                if (implementation is not null)
                    instance.Should().BeOfType(implementation, $"Expected instance ${instance.GetType().FullName} to be of type ${implementation.FullName}!");
            }
        }
    }
}

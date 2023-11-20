using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PiBox.Hosting.Abstractions.Extensions;
using PiBox.Hosting.Abstractions.Plugins;
using PiBox.Hosting.Abstractions.Services;
using PiBox.Plugins.Handlers.Abstractions.Cqrs.Handlers;
using PiBox.Plugins.Handlers.Abstractions.Cqrs.Validators;
using PiBox.Plugins.Handlers.Cqrs.SimpleResource.Handlers;

namespace PiBox.Plugins.Handlers.Cqrs.SimpleResource
{
    public class CqrsSimpleResourcePlugin : IPluginServiceConfiguration
    {
        private readonly IImplementationResolver _implementationResolver;
        private readonly ILogger _logger;

        public CqrsSimpleResourcePlugin(IImplementationResolver implementationResolver, ILogger<CqrsSimpleResourcePlugin> logger)
        {
            _implementationResolver = implementationResolver;
            _logger = logger;
        }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient(typeof(ICreateHandler<>), typeof(SimpleResourceCreateHandler<>));
            serviceCollection.AddTransient(typeof(IUpdateHandler<>), typeof(SimpleResourceUpdateHandler<>));
            serviceCollection.AddTransient(typeof(IDeleteHandler<>), typeof(SimpleResourceDeleteHandler<>));
            serviceCollection.AddTransient(typeof(IGetHandler<>), typeof(SimpleResourceGetHandler<>));
            serviceCollection.AddTransient(typeof(IGetListHandler<>), typeof(SimpleResourceGetListHandler<>));

            foreach (var (interfaceType, implementationType) in FindTypesAndInterfaces<IHandler>())
            {
                _logger.LogDebug("Registering handler {Implementation} for {HandlerType}", implementationType.Name, interfaceType.Name);
                serviceCollection.AddTransient(interfaceType, implementationType);
            }
            foreach (var (interfaceType, implementationType) in FindTypesAndInterfaces<IBaseValidator>())
            {
                _logger.LogDebug("Registering validator {Implementation} for {ValidatorType}", implementationType.Name, interfaceType.Name);
                serviceCollection.AddTransient(interfaceType, implementationType);
            }
        }

        private IEnumerable<(Type interfaceType, Type implementationType)> FindTypesAndInterfaces<T>()
        {
            const string assemblyName = ThisAssembly.AssemblyName;
            var types = _implementationResolver.FindTypes(t => t.Implements<T>() && t.Assembly.GetName().Name != assemblyName);
            foreach (var type in types)
            {
                var interfaces = type.GetInterfaces().Where(x => x != typeof(T) && x.IsAssignableTo(typeof(T)));
                foreach (var interfaceType in interfaces)
                    yield return (interfaceType, type);
            }
        }
    }
}

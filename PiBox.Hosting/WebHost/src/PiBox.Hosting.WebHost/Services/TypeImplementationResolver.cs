using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using PiBox.Hosting.Abstractions.Attributes;
using PiBox.Hosting.Abstractions.Extensions;
using PiBox.Hosting.Abstractions.Plugins;
using PiBox.Hosting.Abstractions.Services;

namespace PiBox.Hosting.WebHost.Services
{
    internal class TypeImplementationResolver : IImplementationResolver
    {
        private readonly IConfiguration _configuration;
        private readonly Type[] _resolvedTypes;
        private readonly IDictionary<Type, object> _defaultArguments;
        private readonly IList<object> _instances = new List<object>();

        public TypeImplementationResolver(IConfiguration configuration, Type[] resolvedTypes, IDictionary<Type, object> defaultArguments)
        {
            _configuration = configuration;
            _resolvedTypes = resolvedTypes;
            defaultArguments.Add(typeof(IImplementationResolver), this);
            defaultArguments.Add(typeof(IConfiguration), configuration);
            _defaultArguments = defaultArguments;
        }

        private object TrackInstance(object instance)
        {
            if (instance is null) return instance;
            _instances.Add(instance);
            return instance;
        }

        private object GetArgument(Type instanceType, Type type)
        {
            if (!_defaultArguments.ContainsKey(type))
            {
                if (type.HasAttribute<ConfigurationAttribute>())
                    return GetConfiguration(type, type.GetAttribute<ConfigurationAttribute>()!.Section);

                var isList = type.GetInterfaces().Any(i => i.IsAssignableTo(typeof(IEnumerable)));
                var innerType = !isList ? type : type.GetElementType() ?? type.GenericTypeArguments[0]!;

                if (innerType.IsInterface && innerType.IsAssignableTo(typeof(IPluginConfigurator)))
                {
                    if (!isList)
                        throw new NotSupportedException(
                            $"For plugin configurators you must implement to take in an enumerable of the configurators. Configurator: {innerType.Name}");
                    var args = _resolvedTypes.Select(x => x.Assembly).Distinct()
                        .SelectMany(x => x.GetTypes())
                        .Where(x => x.IsClass && !x.IsAbstract && x.IsAssignableTo(innerType))
                        .Select(ResolveInstance)
                        .ToList();
                    if (!isList)
                        return args.FirstOrDefault();

                    if (type.IsArray)
                    {
                        var array = Array.CreateInstance(innerType, args.Count);
                        args.ToArray().CopyTo(array, 0);
                        return array;
                    }

                    var listType = typeof(List<>).MakeGenericType(innerType);
                    var list = (IList)Activator.CreateInstance(listType)!;
                    foreach (var item in args)
                        list.Add(item);
                    return list;
                }

                if (!(type.IsGenericType && _defaultArguments.ContainsKey(type.GetGenericTypeDefinition())))
                {
                    return null;
                }

                type = type.GetGenericTypeDefinition();
            }

            var argument = _defaultArguments[type];
            if (argument is Func<Type, object> invocation)
            {
                return invocation.Invoke(instanceType);
            }
            return argument;
        }

        private object GetInstance(Type type) => _instances.FirstOrDefault(x => x.GetType() == type);

        private object GetConfiguration(Type type, string section)
        {
            var configurationInstance = Activator.CreateInstance(type);
            _configuration.Bind(section, configurationInstance);
            return configurationInstance;
        }

        public object ResolveInstance(Type type)
        {
            var existingInstance = GetInstance(type);
            if (existingInstance is not null)
                return existingInstance;
            if (type.HasAttribute<ConfigurationAttribute>())
                return GetConfiguration(type, type.GetAttribute<ConfigurationAttribute>()!.Section);
            var constructor = type.GetConstructors().FirstOrDefault();
            var parameters = constructor?.GetParameters() ?? [];
            if (constructor is null || parameters.Length == 0)
                return TrackInstance(Activator.CreateInstance(type, []));
            var arguments = parameters.Select(parameter => GetArgument(type, parameter.ParameterType)).ToArray();
            return TrackInstance(constructor.Invoke(arguments));
        }

        public List<Type> FindTypes()
        {
            return _resolvedTypes.ToList();
        }

        public List<Assembly> FindAssemblies()
        {
            return _resolvedTypes.Select(x => x.Assembly).Distinct().ToList();
        }

        public void ClearInstances()
        {
            foreach (var disposable in _instances.OfType<IDisposable>())
            {
                disposable.Dispose();
            }
            _instances.Clear();
        }
    }
}

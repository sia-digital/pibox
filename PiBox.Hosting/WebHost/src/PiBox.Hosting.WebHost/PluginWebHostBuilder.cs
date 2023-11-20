using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PiBox.Hosting.Abstractions.Extensions;
using PiBox.Hosting.Abstractions.Plugins;
using PiBox.Hosting.Abstractions.Services;
using PiBox.Hosting.WebHost.Configurators;
using PiBox.Hosting.WebHost.Extensions;
using PiBox.Hosting.WebHost.Logging;
using PiBox.Hosting.WebHost.Services;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace PiBox.Hosting.WebHost
{
    public class PluginWebHostBuilder
    {
        private readonly WebApplicationBuilder _webApplicationBuilder;
        private readonly IImplementationResolver _implementationResolver;
        private readonly ILogger<PluginWebHostBuilder> _logger;

        private readonly Type[] _resolvedTypes;

        // ReSharper disable once MemberCanBePrivate.Global
        public PluginWebHostBuilder(Type[] types, params string[] args)
        {
            SystemExtensions.SetDefaultCultureInfo();
            _webApplicationBuilder = WebApplication.CreateBuilder(args);
            HostConfigurator.ConfigureAppConfiguration(_webApplicationBuilder.Configuration);
            StructuredLoggingExtensions.CreateBootstrapLogger(
                _webApplicationBuilder.Configuration.GetValue<LogEventLevel?>("serilog:minimumLevel")
                ?? LogEventLevel.Information);
            _logger = GetLoggerFactory().CreateLogger<PluginWebHostBuilder>();
            _resolvedTypes = types;
            _implementationResolver = GetImplementationResolver(_webApplicationBuilder);
        }

        public static PluginWebHostBuilder Create(Type[] types, params string[] args) => new(types, args);

        public static void RunDefault(Type[] types, params string[] args)
        {
            Create(types, args).Build().Run();
        }

        public IHost Build()
        {
            var webApplicationConfigurator = new HostConfigurator(_webApplicationBuilder, _logger);
            webApplicationConfigurator.Configure();
            LogPlugins();

            var serviceConfigurator = new ServiceConfigurator(_logger, _webApplicationBuilder.Configuration,
                _webApplicationBuilder.Services, _implementationResolver);
            serviceConfigurator.Configure();
            var host = _webApplicationBuilder.Build();
            var appConfigurator = new AppConfigurator(_logger, host, _implementationResolver);
            appConfigurator.Configure();
            _implementationResolver.ClearInstances();
            return host;
        }

        private static ILoggerFactory GetLoggerFactory() => new SerilogLoggerFactory(Log.Logger);

        private IImplementationResolver GetImplementationResolver(WebApplicationBuilder webApplicationBuilder)
        {
            var loggerFactory = GetLoggerFactory();
            var defaultArgs = new Dictionary<Type, object>
            {
                { typeof(IHostEnvironment), webApplicationBuilder.Environment },
                { typeof(ILogger), new Func<Type, object>(t => loggerFactory.CreateLogger(t)) },
                {
                    typeof(ILogger<>), new Func<Type, object>(t => typeof(LoggerFactoryExtensions).GetMethods()
                        .First(x => x.Name == nameof(LoggerFactoryExtensions.CreateLogger) &&
                                    x.IsGenericMethodDefinition)
                        .MakeGenericMethod(t)
                        .Invoke(null, new object[] { loggerFactory })!)
                },
                { typeof(ILoggerFactory), loggerFactory }
            };

            // use type resolving from source generator so we can use native AOT compilation
            return new TypeImplementationResolver(webApplicationBuilder.Configuration, _resolvedTypes, defaultArgs);
        }

        private void LogPlugins()
        {
            var pluginActivateables = _implementationResolver.FindTypes(x => x.Implements<IPluginActivateable>());
            _logger.LogInformation("Found {PluginCount} plugins", pluginActivateables.Count);
            pluginActivateables.ForEach(activateable =>
            {
                _logger.LogInformation("Loaded plugin {ActivateablesText}", activateable.GetPluginName());
            });
        }
    }
}

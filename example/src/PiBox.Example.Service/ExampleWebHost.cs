using System.Diagnostics;
using PiBox.Hosting.Abstractions.Extensions;
using PiBox.Hosting.Abstractions.Plugins;
using PiBox.Hosting.Abstractions.Services;

namespace PiBox.Example.Service
{
    public class ExampleWebHost : IPluginEndpointsConfiguration, IPluginServiceConfiguration
    {
        private readonly IImplementationResolver _implementationResolver;
        private readonly ILogger _logger;

        public ExampleWebHost(IImplementationResolver implementationResolver, ILogger logger)
        {
            _implementationResolver = implementationResolver;
            _logger = logger;
        }

        public void ConfigureEndpoints(IEndpointRouteBuilder endpointRouteBuilder, IServiceProvider serviceProvider)
        {
            var myActivitySource = new ActivitySource("WebHostSamplePlugin");

            endpointRouteBuilder.MapGet("/hello", () =>
            {
                using (var activity = myActivitySource.StartActivity("SayHello"))
                {
                    activity?.SetTag("foo", 1);
                    activity?.SetTag("bar", "Hello, World!");
                    activity?.SetTag("baz", new int[] { 1, 2, 3 });
                }

                return "Hello, World!";
            });

            endpointRouteBuilder.MapGet("/secure", () =>
            {
                using (var activity = myActivitySource.StartActivity("SayHelloSecure"))
                {
                    activity?.SetTag("foo", 1);
                    activity?.SetTag("bar", "Hello, secure World!");
                    activity?.SetTag("baz", new int[] { 1, 2, 3 });
                }

                return "Hello, secure World!";
            }).RequireAuthorization();

            endpointRouteBuilder.MapGet("/delay", async () =>
            {
                using (var activity = myActivitySource.StartActivity("delay"))
                {
                    activity?.SetTag("foo", 1);
                    activity?.SetTag("bar", "delay");
                    activity?.SetTag("baz", new int[] { 1, 2, 3 });
                }

                await Task.Delay(5000, CancellationToken.None);

                using (var activity = myActivitySource.StartActivity("delay-end"))
                {
                    activity?.SetTag("foo", 2);
                    activity?.SetTag("bar", "delay");
                }

                return "done!";
            });
        }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            var x = _implementationResolver.FindAndResolve<IPluginActivateable>();
            _logger.LogDebug("Found {ItemCount} plugins", x.Count);
        }
    }
}

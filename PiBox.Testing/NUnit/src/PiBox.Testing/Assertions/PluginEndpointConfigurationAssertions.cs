using System.Collections.ObjectModel;
using System.Globalization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using PiBox.Hosting.Abstractions.Plugins;

namespace PiBox.Testing.Assertions
{
    public static class PluginEndpointConfigurationAssertions
    {
        public static void ShouldHaveEndpoints(this IPluginEndpointsConfiguration endpointsConfiguration,
            IDictionary<string, HttpMethod[]> routes,
            IServiceProvider serviceProvider = null)
        {
            serviceProvider ??= Substitute.For<IServiceProvider>();
            var routeBuilder = Substitute.For<IEndpointRouteBuilder>();
            var endpoints = new Collection<EndpointDataSource>();
            routeBuilder.DataSources.Returns(endpoints);
            endpointsConfiguration.ConfigureEndpoints(routeBuilder, serviceProvider);
            var appliedRoutes = endpoints.SelectMany(x => x.Endpoints).OfType<RouteEndpoint>().ToList();
            foreach (var (expectedRoute, methods) in routes)
            {
                foreach (var method in methods)
                {
                    var methodName = method.ToString().ToUpper(CultureInfo.InvariantCulture);
                    var route = appliedRoutes.FirstOrDefault(x => x.RoutePattern.RawText == expectedRoute && x.ContainsMethod(methodName))!;
                    route.Should().NotBeNull($"Found no applied route '{route}' for method '{methodName}'");
                }
            }
        }

        public static RouteEndpoint GetEndpoint(this IPluginEndpointsConfiguration endpointsConfiguration, HttpMethod method, string route, IServiceProvider serviceProvider = null)
        {
            serviceProvider ??= Substitute.For<IServiceProvider>();
            var routeBuilder = Substitute.For<IEndpointRouteBuilder>();
            var endpoints = new Collection<EndpointDataSource>();
            routeBuilder.DataSources.Returns(endpoints);
            endpointsConfiguration.ConfigureEndpoints(routeBuilder, serviceProvider);
            var appliedRoutes = endpoints.SelectMany(x => x.Endpoints).OfType<RouteEndpoint>().ToList();
            var methodName = method.ToString().ToUpper(CultureInfo.InvariantCulture);
            return appliedRoutes.Single(x => x.RoutePattern.RawText == route && x.ContainsMethod(methodName));
        }

        private static bool ContainsMethod(this Endpoint routeEndpoint, string methodName)
        {
            var metadata = routeEndpoint.Metadata.GetMetadata<HttpMethodMetadata>()!;
            metadata.Should().NotBeNull($"Metadata for ${routeEndpoint.DisplayName} should not be null!");
            return metadata.HttpMethods.Select(x => x.ToUpper(CultureInfo.InvariantCulture)).Contains(methodName);
        }
    }
}

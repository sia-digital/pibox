using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using PiBox.Plugins.Endpoints.RestResourceEntity.Endpoints;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Endpoints.RestResourceEntity.Extensions
{
    public static class RestSimpleResourceEntityEndpointExtensions
    {
        public static RestSimpleResourceEndpointBuilder<T> AddSimpleRestResource<T>(this IEndpointRouteBuilder endpointRouteBuilder, IServiceProvider serviceProvider, string resource) where T : class, IGuidIdentifier
        {
            var defaultResponses = serviceProvider.GetRequiredService<GlobalResponseOptions>();
            return new RestSimpleResourceEndpointBuilder<T>(endpointRouteBuilder, resource, defaultResponses);
        }
    }
}

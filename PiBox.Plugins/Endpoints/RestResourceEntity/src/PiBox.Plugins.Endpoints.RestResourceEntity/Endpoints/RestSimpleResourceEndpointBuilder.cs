using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PiBox.Extensions.Abstractions;
using PiBox.Hosting.Abstractions.Middlewares.Models;
using PiBox.Plugins.Handlers.Abstractions.Cqrs;
using PiBox.Plugins.Persistence.Abstractions;
// ReSharper disable RedundantArgumentDefaultValue

namespace PiBox.Plugins.Endpoints.RestResourceEntity.Endpoints
{
    public class RestSimpleResourceEndpointBuilder<T> where T : class, IGuidIdentifier
    {
        private readonly IEndpointRouteBuilder _endpointRouteBuilder;
        private readonly string _resource;
        private readonly GlobalResponseOptions _globalResponseOptions;

        public RestSimpleResourceEndpointBuilder(IEndpointRouteBuilder endpointRouteBuilder, string resource,
            GlobalResponseOptions globalResponseOptions)
        {
            _endpointRouteBuilder = endpointRouteBuilder;
            _resource = resource;
            _globalResponseOptions = globalResponseOptions;
        }

        private void AddDefaultResponses(RouteHandlerBuilder routeBuilder)
        {
            foreach (var (statusCode, type) in _globalResponseOptions.DefaultResponses)
            {
                if (type == null) routeBuilder.Produces((int)statusCode);
                else routeBuilder.Produces((int)statusCode, type);
            }
        }

        private static string[] GetPolicies(ICollection<string> policyNames)
        {
            if (!policyNames.Contains("DEFAULT"))
            {
                policyNames.Add("DEFAULT");
            }

            return policyNames.ToArray();
        }

        private RestSimpleResourceEndpointConfiguration GetConfiguration(
            Action<RestSimpleResourceEndpointConfiguration> configure)
        {
            var config = new RestSimpleResourceEndpointConfiguration(_resource);
            configure?.Invoke(config);
            return config;
        }

        private void ConfigureRoute(HandlerAction action, RouteHandlerBuilder route,
            RestSimpleResourceEndpointConfiguration config)
        {
            var openApiTags = config.OpenApiTags.Any() ? config.OpenApiTags.ToArray() : new[] { _resource };
            route.WithTags(openApiTags);
            route.WithName($"{action}-{config.ResourceName}");
            if (!string.IsNullOrEmpty(config.DisplayName))
                route.WithDisplayName(config.DisplayName);
            if (!string.IsNullOrEmpty(config.GroupName))
                route.WithGroupName(config.GroupName);
            if (config.AllowAnonymousEnabled)
                route.AllowAnonymous();
            else
                route.RequireAuthorization(GetPolicies(config.Policies));
            AddDefaultResponses(route);
            foreach (var (statusCode, type) in config.ProducesMap)
            {
                route.Produces(statusCode, type);
            }
        }

        private RouteHandlerBuilder GetRoute(HandlerAction action,
            RestSimpleResourceEndpointConfiguration configuration, Delegate handler)
        {
            var route = action switch
            {
                HandlerAction.Create => _endpointRouteBuilder.MapPost($"/{_resource}", handler),
                HandlerAction.Get => _endpointRouteBuilder.MapGet($"/{_resource}/{{id:guid}}", handler),
                HandlerAction.GetList => _endpointRouteBuilder.MapGet($"/{_resource}", handler),
                HandlerAction.Update => _endpointRouteBuilder.MapPut($"/{_resource}/{{id:guid}}", handler),
                HandlerAction.Delete => _endpointRouteBuilder.MapDelete($"/{_resource}/{{id:guid}}", handler),
                _ => throw new ArgumentOutOfRangeException(nameof(action), action, "HandlerAction does not exist")
            };
            ConfigureRoute(action, route, configuration);
            return route;
        }

        public void ForAll(Action<RestSimpleResourceEndpointConfiguration> configureGetList = null,
            Action<RestSimpleResourceEndpointConfiguration> configureGet = null,
            Action<RestSimpleResourceEndpointConfiguration> configurePost = null,
            Action<RestSimpleResourceEndpointConfiguration> configurePut = null,
            Action<RestSimpleResourceEndpointConfiguration> configureDelete = null)
        {
            ForGetList(configureGetList);
            ForGet(configureGet);
            ForPost(configurePost);
            ForPut(configurePut);
            ForDelete(configureDelete);
        }

        public RestSimpleResourceEndpointBuilder<T> ForGetList(
            Action<RestSimpleResourceEndpointConfiguration> configure = null)
        {
            var config = GetConfiguration(configure);
            var route = GetRoute(HandlerAction.GetList, config, RestActions.GetListAsync<T>);
            if (!config.DefaultResponsesEnabled) return this;
            route.Produces<PagedList<T>>();
            route.Produces<ErrorResponse>(StatusCodes.Status404NotFound);

            return this;
        }

        public RestSimpleResourceEndpointBuilder<T> ForGet(
            Action<RestSimpleResourceEndpointConfiguration> configure = null)
        {
            var config = GetConfiguration(configure);
            var route = GetRoute(HandlerAction.Get, config, RestActions.GetAsync<T>);
            if (!config.DefaultResponsesEnabled) return this;
            route.Produces<T>(StatusCodes.Status200OK);
            route.Produces<ErrorResponse>(StatusCodes.Status404NotFound);
            return this;
        }

        public RestSimpleResourceEndpointBuilder<T> ForPost(
            Action<RestSimpleResourceEndpointConfiguration> configure = null)
        {
            var config = GetConfiguration(configure);
            var route = GetRoute(HandlerAction.Create, config, RestActions.CreateAsync<T>);
            if (!config.DefaultResponsesEnabled) return this;
            route.Produces<T>(StatusCodes.Status201Created);
            route.Produces<ErrorResponse>(StatusCodes.Status404NotFound);
            route.Produces<ErrorResponse>(StatusCodes.Status413PayloadTooLarge);
            return this;
        }

        public RestSimpleResourceEndpointBuilder<T> ForPut(
            Action<RestSimpleResourceEndpointConfiguration> configure = null)
        {
            var config = GetConfiguration(configure);
            var route = GetRoute(HandlerAction.Update, config, RestActions.UpdateAsync<T>);
            if (!config.DefaultResponsesEnabled) return this;
            route.Produces<T>(StatusCodes.Status200OK);
            route.Produces<ErrorResponse>(StatusCodes.Status404NotFound);
            route.Produces<ErrorResponse>(StatusCodes.Status412PreconditionFailed);
            route.Produces<ErrorResponse>(StatusCodes.Status409Conflict);
            route.Produces<ErrorResponse>(StatusCodes.Status413PayloadTooLarge);
            return this;
        }

        public RestSimpleResourceEndpointBuilder<T> ForDelete(
            Action<RestSimpleResourceEndpointConfiguration> configure = null)
        {
            var config = GetConfiguration(configure);
            var route = GetRoute(HandlerAction.Delete, config, RestActions.DeleteAsync<T>);
            if (!config.DefaultResponsesEnabled) return this;
            route.Produces(StatusCodes.Status204NoContent);
            route.Produces<ErrorResponse>(StatusCodes.Status404NotFound);
            route.Produces<ErrorResponse>(StatusCodes.Status412PreconditionFailed);
            route.Produces<ErrorResponse>(StatusCodes.Status409Conflict);

            return this;
        }
    }
}

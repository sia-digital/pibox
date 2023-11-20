using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PiBox.Extensions.Abstractions;
using PiBox.Plugins.Handlers.Abstractions.Cqrs.Handlers;
using PiBox.Plugins.Handlers.Abstractions.Cqrs.Models;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Endpoints.RestResourceEntity.Endpoints
{
    public static class RestActions
    {
        public static async Task<IResult> GetListAsync<T>(
            [FromQuery] int? size,
            [FromQuery] int? page,
            [FromQuery] string filter,
            [FromQuery] string sort,
            [FromServices] IGetListHandler<T> handler,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            CancellationToken cancellationToken)
        {
            var request = new PagingRequest(size, page, filter, sort);
            var result = await handler.GetListAsync(request, cancellationToken);
            return Results.Ok(result);
        }

        public static async Task<IResult> GetAsync<T>(
            [FromRoute] Guid id,
            [FromServices] IGetHandler<T> handler,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            CancellationToken cancellationToken)
        {
            var result = await handler.GetAsync(new GuidIdentifier(id), cancellationToken);
            return Results.Ok(result);
        }

        public static async Task<IResult> CreateAsync<T>(
            [FromBody] T model,
            [FromServices] ICreateHandler<T> handler,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            CancellationToken cancellationToken)
        {
            var result = await handler.CreateAsync(model!, cancellationToken);
            return Results.Ok(result);
        }

        public static async Task<IResult> UpdateAsync<T>(
            [FromRoute] Guid id,
            [FromBody] T model,
            [FromServices] IUpdateHandler<T> handler,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            CancellationToken cancellationToken)
        {
            if (model is IGuidIdentifier guidIdentifier)
                guidIdentifier.Id = id;
            var result = await handler.UpdateAsync(model!, cancellationToken);
            return Results.Ok(result);
        }

        public static async Task<IResult> DeleteAsync<T>(
            [FromRoute] Guid id,
            [FromServices] IDeleteHandler<T> handler,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            CancellationToken cancellationToken)
        {
            await handler.DeleteAsync(new GuidIdentifier(id), cancellationToken);
            return Results.NoContent();
        }
    }
}

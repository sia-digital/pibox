using System.Diagnostics;
using FluentValidation;
using PiBox.Extensions.Abstractions;
using PiBox.Hosting.Abstractions.DependencyInjection;
using PiBox.Plugins.Handlers.Abstractions.Cqrs.Handlers;
using PiBox.Plugins.Handlers.Abstractions.Cqrs.Validators;
using PiBox.Plugins.Handlers.Cqrs.SimpleResource.Validators;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Handlers.Cqrs.SimpleResource.Handlers
{
    public class SimpleResourceGetListHandler<TResource> : IGetListHandler<TResource>
        where TResource : class, IGuidIdentifier
    {
        private readonly ActivitySource _activitySource = new($"{nameof(SimpleResourceGetListHandler<TResource>)}<{typeof(TResource)}>");
        private readonly IFactory<IReadRepository<TResource>> _repositoryFactory;
        private readonly IValidator<PagingRequest> _validator;

        public SimpleResourceGetListHandler(IFactory<IReadRepository<TResource>> repositoryFactory)
        {
            _repositoryFactory = repositoryFactory;
            _validator = new GenericValidator<PagingRequest>(PagingRequestValidator.Validate);
        }

        public async Task<PagedList<TResource>> GetListAsync(PagingRequest request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity(nameof(GetListAsync), kind: ActivityKind.Internal, parentContext: default);
            await _validator.ValidateOrThrowAsync(request, cancellationToken);
            var queryOptions = QueryOptions<TResource>.Parse(request.Filter, request.Sort, request.Size, request.Page);
            var totalCountTask = _repositoryFactory.Create().CountAsync(queryOptions, cancellationToken);
            var resultTask = _repositoryFactory.Create().FindAsync(queryOptions, cancellationToken);
            var totalCount = await totalCountTask;
            var result = await resultTask;
            return new PagedList<TResource>(result, totalCount, queryOptions.Page.Value, queryOptions.Size.Value);
        }
    }
}

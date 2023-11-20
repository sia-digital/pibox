using System.Diagnostics;
using FluentValidation;
using PiBox.Plugins.Handlers.Abstractions.Cqrs.Handlers;
using PiBox.Plugins.Handlers.Abstractions.Cqrs.Validators;
using PiBox.Plugins.Handlers.Cqrs.SimpleResource.Validators;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Handlers.Cqrs.SimpleResource.Handlers
{
    public class SimpleResourceGetHandler<TResource> : IGetHandler<TResource>
        where TResource : class, IGuidIdentifier
    {
        private readonly ActivitySource _activitySource = new($"{nameof(SimpleResourceGetHandler<TResource>)}<{typeof(TResource)}>");
        private readonly IReadRepository<TResource> _repository;
        private readonly IValidator<IGuidIdentifier> _validator;

        public SimpleResourceGetHandler(IReadRepository<TResource> repository)
        {
            _repository = repository;
            _validator = new GenericValidator<IGuidIdentifier>(GuidIdentifierValidator.Validate);
        }

        public async Task<TResource> GetAsync(IGuidIdentifier request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity(nameof(GetAsync), kind: ActivityKind.Internal, parentContext: default);
            activity!.AddTag("resource-id", request);
            await _validator.ValidateOrThrowAsync(request, cancellationToken);
            return await _repository.FindByIdAsync(request.Id, cancellationToken);
        }
    }
}

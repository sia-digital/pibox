using System.Diagnostics;
using FluentValidation;
using PiBox.Plugins.Handlers.Abstractions.Cqrs.Handlers;
using PiBox.Plugins.Handlers.Abstractions.Cqrs.Validators;
using PiBox.Plugins.Handlers.Cqrs.SimpleResource.Validators;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Handlers.Cqrs.SimpleResource.Handlers
{
    public class SimpleResourceDeleteHandler<TResource> : IDeleteHandler<IGuidIdentifier> where TResource : class, IGuidIdentifier
    {
        private readonly ActivitySource _activitySource = new($"{nameof(SimpleResourceDeleteHandler<TResource>)}<{typeof(TResource)}>");
        private readonly IRepository<TResource> _repository;
        private readonly IValidator<IGuidIdentifier> _validator;

        public SimpleResourceDeleteHandler(IRepository<TResource> repository)
        {
            _repository = repository;
            _validator = new GenericValidator<IGuidIdentifier>(GuidIdentifierValidator.Validate);
        }

        public async Task DeleteAsync(IGuidIdentifier request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity(nameof(DeleteAsync), kind: ActivityKind.Internal, parentContext: default);
            activity!.AddTag("resource-id", request.Id);
            await _validator.ValidateOrThrowAsync(request, cancellationToken);
            await _repository.RemoveAsync(request.Id, cancellationToken);
        }
    }
}

using System.Diagnostics;
using FluentValidation;
using PiBox.Hosting.Abstractions.Exceptions;
using PiBox.Plugins.Handlers.Abstractions.Cqrs.Handlers;
using PiBox.Plugins.Handlers.Abstractions.Cqrs.Validators;
using PiBox.Plugins.Handlers.Cqrs.SimpleResource.Validators;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Handlers.Cqrs.SimpleResource.Handlers
{
    public class SimpleResourceUpdateHandler<TResource> : IUpdateHandler<TResource> where TResource : class, IGuidIdentifier
    {
        private readonly ActivitySource _activitySource = new($"{nameof(SimpleResourceUpdateHandler<TResource>)}<{typeof(TResource)}>");
        private readonly IRepository<TResource> _repository;
        private readonly IValidator<TResource> _validator;

        public SimpleResourceUpdateHandler(IRepository<TResource> repository, IUpdateValidator<TResource> validator)
        {
            _repository = repository;
            _validator = new GenericValidator<TResource>(validator.ValidateOnUpdate);
        }

        public async Task<TResource> UpdateAsync(TResource request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity(nameof(UpdateAsync), kind: ActivityKind.Internal, parentContext: default);
            activity!.AddTag("resource-id", request.Id);
            await _validator.ValidateOrThrowAsync(request, cancellationToken);
            var existingResource = await _repository.FindByIdAsync(request.Id, cancellationToken);
            if (existingResource is null)
                throw new PiBoxException($"Could not find {typeof(TResource).Name} for id '{request.Id}'");
            return await _repository.UpdateAsync(request, cancellationToken);
        }
    }
}

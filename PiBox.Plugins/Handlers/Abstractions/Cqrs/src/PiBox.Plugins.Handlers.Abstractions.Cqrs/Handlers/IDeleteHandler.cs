using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Handlers.Abstractions.Cqrs.Handlers
{
    // ReSharper disable once UnusedTypeParameter
#pragma warning disable S2326
    public interface IDeleteHandler<in TResource> : IHandler
    {
        Task DeleteAsync(IGuidIdentifier request, CancellationToken cancellationToken);
    }
#pragma warning restore S2326
}

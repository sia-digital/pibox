using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Handlers.Abstractions.Cqrs.Handlers
{
    public interface IGetHandler<TResource> : IHandler
    {
        Task<TResource> GetAsync(IGuidIdentifier request, CancellationToken cancellationToken);
    }
}

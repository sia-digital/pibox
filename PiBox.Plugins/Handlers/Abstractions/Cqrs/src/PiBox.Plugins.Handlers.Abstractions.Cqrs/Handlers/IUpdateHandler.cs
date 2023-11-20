namespace PiBox.Plugins.Handlers.Abstractions.Cqrs.Handlers
{
    public interface IUpdateHandler<TResource> : IHandler
    {
        Task<TResource> UpdateAsync(TResource request, CancellationToken cancellationToken);
    }
}

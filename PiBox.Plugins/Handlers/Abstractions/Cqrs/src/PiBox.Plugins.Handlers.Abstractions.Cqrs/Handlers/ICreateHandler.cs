namespace PiBox.Plugins.Handlers.Abstractions.Cqrs.Handlers
{
    public interface ICreateHandler<TResource> : IHandler
    {
        Task<TResource> CreateAsync(TResource request, CancellationToken cancellationToken);
    }
}

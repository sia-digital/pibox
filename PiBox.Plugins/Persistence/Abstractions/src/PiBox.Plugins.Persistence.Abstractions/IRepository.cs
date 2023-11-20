namespace PiBox.Plugins.Persistence.Abstractions
{
    public interface IRepository<TEntity> : IReadRepository<TEntity>
        where TEntity : class, IGuidIdentifier
    {
        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
    }
}

namespace PiBox.Plugins.Persistence.Abstractions
{
    public interface IReadRepository<TEntity>
        where TEntity : class, IGuidIdentifier
    {
        Task<TEntity> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<TEntity>> FindAsync(QueryOptions<TEntity> queryOptions, CancellationToken cancellationToken = default);
        Task<int> CountAsync(QueryOptions<TEntity> queryOptions, CancellationToken cancellationToken = default);
    }
}

using System.Diagnostics;
using Chronos.Abstractions;
using Microsoft.EntityFrameworkCore;
using PiBox.Hosting.Abstractions.Exceptions;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Persistence.EntityFramework
{
    public class EntityFrameworkRepository<TEntity> : IRepository<TEntity> where TEntity : class, IGuidIdentifier
    {
        private readonly ActivitySource _activitySource = new($"{nameof(EntityFrameworkRepository<TEntity>)}<{typeof(TEntity)}>");
        private readonly List<KeyValuePair<string, object>> _activitySourceTags = new() { new("persistence", "entityframework") };
        private readonly IDbContext<TEntity> _dbContext;
        private readonly IDateTimeProvider _dateTimeProvider;

        public EntityFrameworkRepository(IDbContext<TEntity> dbContext, IDateTimeProvider dateTimeProvider)
        {
            _dbContext = dbContext;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<TEntity> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity(nameof(FindByIdAsync), kind: ActivityKind.Internal, parentContext: default,
                _activitySourceTags!);
            activity!.AddTag("resource-id", id);
            var entity = await _dbContext.GetSet().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null)
                throw new PiBoxException($"Could not find {typeof(TEntity).Name} for id '{id}'", 404);
            return entity;
        }

        public async Task<List<TEntity>> FindAsync(QueryOptions<TEntity> queryOptions,
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity(nameof(FindAsync), kind: ActivityKind.Internal, parentContext: default,
                _activitySourceTags!);
            return await _dbContext.GetSet().WithQueryOptions(queryOptions).ToListAsync(cancellationToken);
        }

        public async Task<int> CountAsync(QueryOptions<TEntity> queryOptions, CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity(nameof(CountAsync), kind: ActivityKind.Internal, parentContext: default,
                _activitySourceTags!);
            return await _dbContext.GetSet().WithQueryOptionsForCount(queryOptions).CountAsync(cancellationToken);
        }

        public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity(nameof(AddAsync), kind: ActivityKind.Internal, parentContext: default, _activitySourceTags!);
            entity.Id = Guid.NewGuid();
            if (entity is ICreationDate entityWithCreationData)
                entityWithCreationData.CreationDate = _dateTimeProvider.UtcNow;
            var entry = await _dbContext.GetSet().AddAsync(entity, cancellationToken);
            await _dbContext.GetContext().SaveChangesAsync(cancellationToken);
            return entry.Entity;
        }

        public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity(nameof(UpdateAsync), kind: ActivityKind.Internal, parentContext: default,
                _activitySourceTags!);
            activity!.AddTag("resource-id", entity.Id);
            var dbSet = _dbContext.GetSet();
            var current = await dbSet.FirstOrDefaultAsync(x => x.Id == entity.Id, cancellationToken);
            if (current is null)
                throw new PiBoxException($"Could not find {typeof(TEntity).Name} for id '{entity.Id}'", 404);
            if (entity is ICreationDate creationDate)
                creationDate.CreationDate = ((ICreationDate)current).CreationDate;
            _dbContext.GetContext().Entry(current).CurrentValues.SetValues(entity);
            await _dbContext.GetContext().SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity(nameof(RemoveAsync), kind: ActivityKind.Internal, parentContext: default,
                _activitySourceTags!);
            activity!.AddTag("resource-id", id);
            var dbSet = _dbContext.GetSet();
            var current = await dbSet.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (current is null)
                throw new PiBoxException($"Could not find {typeof(TEntity).Name} for id '{id}'", 404);
            _dbContext.GetSet().Remove(current);
            await _dbContext.GetContext().SaveChangesAsync(cancellationToken);
        }
    }
}

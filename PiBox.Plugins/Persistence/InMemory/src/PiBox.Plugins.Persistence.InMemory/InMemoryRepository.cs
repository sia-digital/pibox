using System.Diagnostics;
using Chronos.Abstractions;
using PiBox.Hosting.Abstractions.Exceptions;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Persistence.InMemory
{
    public class InMemoryRepository<TEntity> : IRepository<TEntity>
        where TEntity : class, IGuidIdentifier
    {
        private readonly ActivitySource _activitySource = new($"{nameof(InMemoryRepository<TEntity>)}<{typeof(TEntity)}>");
        private readonly List<KeyValuePair<string, object>> _activitySourceTags = new() { new("persistence", "in-memory") };
        private readonly HashSet<TEntity> _data;
        private readonly IDateTimeProvider _dateTimeProvider;

        public InMemoryRepository(IDateTimeProvider dateTimeProvider, InMemoryStore store)
        {
            _dateTimeProvider = dateTimeProvider;
            _data = store.GetStore<TEntity>();
        }

        public Task<TEntity> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity(nameof(FindByIdAsync), kind: ActivityKind.Internal, parentContext: default,
                _activitySourceTags!);
            activity!.AddTag("resource-id", id);
            var entity = _data.FirstOrDefault(x => x.Id == id);
            if (entity is null)
                throw new PiBoxException($"Could not find entity for id '{id}'", 404);
            return Task.FromResult(entity);
        }

        public Task<List<TEntity>> FindAsync(QueryOptions<TEntity> queryOptions,
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity(nameof(FindAsync), kind: ActivityKind.Internal, parentContext: default,
                _activitySourceTags!);

            var result = _data.AsQueryable().WithQueryOptions(queryOptions).ToList();
            return Task.FromResult(result);
        }

        public Task<int> CountAsync(QueryOptions<TEntity> queryOptions, CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity(nameof(CountAsync), kind: ActivityKind.Internal, parentContext: default,
                _activitySourceTags!);

            return Task.FromResult(_data.AsQueryable().WithQueryOptionsForCount(queryOptions).Count());
        }

        public Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            entity.Id = Guid.NewGuid();
            using var activity = _activitySource.StartActivity(nameof(AddAsync), kind: ActivityKind.Internal, parentContext: default,
                _activitySourceTags!);
            activity!.AddTag("resource-id", entity.Id);
            if (entity is ICreationDate entityWithCreationData)
                entityWithCreationData.CreationDate = _dateTimeProvider.UtcNow;
            _data.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity(nameof(UpdateAsync), kind: ActivityKind.Internal, parentContext: default,
                _activitySourceTags!);
            activity!.AddTag("resource-id", entity.Id);
            var current = _data.FirstOrDefault(x => x.Id == entity.Id);
            if (current is null)
            {
                throw new PiBoxException($"The entity with the id '{entity.Id}' does not exist.", 404);
            }
            if (entity is ICreationDate creationDate)
            {
#pragma warning disable S2259
                creationDate.CreationDate = ((ICreationDate)current).CreationDate;
#pragma warning restore S2259
            }
            _data.Remove(current);
            _data.Add(entity);
            return Task.FromResult(entity);
        }

        public Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity(nameof(RemoveAsync), kind: ActivityKind.Internal, parentContext: default,
                _activitySourceTags!);
            activity!.AddTag("resource-id", id);
            var current = _data.FirstOrDefault(x => x.Id == id);
            if (current is null)
                throw new PiBoxException($"The entity with the id '{id}' does not exist.", 404);
            _data.Remove(current);
            return Task.CompletedTask;
        }
    }
}

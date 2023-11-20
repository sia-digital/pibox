using System.Diagnostics;
using Chronos.Abstractions;
using MongoDB.Driver;
using ODataQueryHelper.Core.Model;
using PiBox.Hosting.Abstractions.Exceptions;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Persistence.MongoDb
{
    public class MongoRepository<TEntity> : IRepository<TEntity> where TEntity : class, IGuidIdentifier
    {
        private readonly ActivitySource _activitySource = new($"{nameof(MongoRepository<TEntity>)}<{typeof(TEntity)}>");
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IMongoCollection<TEntity> _collection;
        private readonly List<KeyValuePair<string, object>> _activitySourceTags = new() { new("persistence", "mongodb") };

        public MongoRepository(IMongoDbInstance mongoDbInstance, IDateTimeProvider dateTimeProvider)
        {
            _dateTimeProvider = dateTimeProvider;
            _collection = mongoDbInstance.GetCollectionFor<TEntity>();
        }

        public async Task<TEntity> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity(nameof(FindByIdAsync), kind: ActivityKind.Internal, parentContext: default,
                _activitySourceTags!);
            activity!.AddTag("resource-id", id);
            var findOpts = new FindOptions<TEntity> { Limit = 1, Skip = 0 };
            var asyncCursor = await _collection.FindAsync(entity => entity.Id == id, findOpts, cancellationToken);
            return await asyncCursor.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<TEntity>> FindAsync(QueryOptions<TEntity> queryOptions,
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity(nameof(FindAsync), kind: ActivityKind.Internal, parentContext: default,
                _activitySourceTags!);
            var findOpts = GetFindOptions(queryOptions);
            var asyncCursor =
                await _collection.FindAsync(GetFilterDefinition(queryOptions), findOpts, cancellationToken);
            return await asyncCursor.ToListAsync(cancellationToken);
        }

        public async Task<int> CountAsync(QueryOptions<TEntity> queryOptions,
            CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity(nameof(CountAsync), kind: ActivityKind.Internal, parentContext: default,
                _activitySourceTags!);
            var count = await _collection.CountDocumentsAsync(GetFilterDefinition(queryOptions),
                cancellationToken: cancellationToken);
            return Convert.ToInt32(count);
        }

        public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            using var updateActivity = _activitySource.StartActivity(nameof(AddAsync), kind: ActivityKind.Internal, parentContext: default,
                _activitySourceTags!);
            if (entity is ICreationDate entityWithCreationDate)
                entityWithCreationDate.CreationDate = _dateTimeProvider.UtcNow;
            await _collection.InsertOneAsync(entity, new InsertOneOptions(), cancellationToken);
            return entity;
        }

        public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity(nameof(UpdateAsync), kind: ActivityKind.Internal, parentContext: default,
                _activitySourceTags!);
            activity!.AddTag("resource-id", entity.Id);
            await _collection.ReplaceOneAsync(e => e.Id == entity.Id, entity, cancellationToken: cancellationToken);
            return entity;
        }

        public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var activity = _activitySource.StartActivity(nameof(RemoveAsync), kind: ActivityKind.Internal, parentContext: default,
                _activitySourceTags!);
            activity!.AddTag("resource-id", id);
            var deleteOneAsync = await _collection.DeleteOneAsync(f => f.Id == id, cancellationToken);
            if (deleteOneAsync.IsAcknowledged && deleteOneAsync.DeletedCount > 0)
            {
                return;
            }

            throw new PiBoxException($"The {typeof(TEntity).Name} with the id '{id}' does not exist.", 404);
        }

        private static FindOptions<TEntity> GetFindOptions(QueryOptions<TEntity> queryOptions)
        {
            var findOpts = new FindOptions<TEntity> { Limit = queryOptions.Size.Value, Skip = queryOptions.Page.Value * queryOptions.Size.Value };
            if (!queryOptions.OrderByExpressions.Any()) return findOpts;
            var sortBuilder = new SortDefinitionBuilder<TEntity>();
            var sortDefinitions = queryOptions.OrderByExpressions.Select(x =>
                    x.Direction == OrderByDirectionType.Ascending
                        ? sortBuilder.Ascending(x.Expression)
                        : sortBuilder.Descending(x.Expression))
                .ToList();
            findOpts.Sort = sortBuilder.Combine(sortDefinitions);
            return findOpts;
        }

        private static FilterDefinition<TEntity> GetFilterDefinition(QueryOptions<TEntity> queryOptions)
        {
            return queryOptions.FilterExpression is null
                ? FilterDefinition<TEntity>.Empty
                : new FilterDefinitionBuilder<TEntity>().Where(queryOptions.FilterExpression);
        }
    }
}

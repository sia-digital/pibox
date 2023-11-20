using System.Globalization;
using Chronos;
using Chronos.Abstractions;
using FluentAssertions;
using MongoDB.Driver;
using NSubstitute;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Exceptions;
using PiBox.Plugins.Persistence.Abstractions;
using PiBox.Testing;

namespace PiBox.Plugins.Persistence.MongoDb
{
    [TestFixture]
    public class MongoDbRepositoryTests
    {
        private readonly IDateTimeProvider _dateTimeProvider = new DateTimeProvider();

        private readonly List<TestEntity> _mockData = new()
        {
            new TestEntity(Guid.Parse("CD8E1D0F-6C70-402A-BDAC-3E782A1BA596"), "TestEntity-1",
                DateTime.Parse("2000-01-01T12:00:00.000Z", CultureInfo.InvariantCulture)),
            new TestEntity(Guid.Parse("73B09FC5-158A-4B72-A968-D4E9312B59BD"), "TestEntity-2",
                DateTime.Parse("2000-01-01T12:00:00.000Z", CultureInfo.InvariantCulture))
        };

        private IMongoDbInstance _mongoDbInstanceMock = null!;
        private IMongoCollection<TestEntity> _mongoDbCollectionMock = null!;

        [SetUp]
        public void SetUp()
        {
            ActivityTestBootstrapper.Setup();

            var cursor = Substitute.For<IAsyncCursor<TestEntity>>();
            cursor.Current.Returns(_mockData);
            cursor.MoveNext(Arg.Any<CancellationToken>()).Returns(true, false);
            cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);

            _mongoDbCollectionMock = Substitute.For<IMongoCollection<TestEntity>>();
            _mongoDbCollectionMock.InsertOneAsync(
                Arg.Any<TestEntity>(),
                Arg.Any<InsertOneOptions>(), CancellationToken.None);
            _mongoDbCollectionMock.UpdateOneAsync(Arg.Any<FilterDefinition<TestEntity>>(),
                Arg.Any<UpdateDefinition<TestEntity>>(), new UpdateOptions(),
                CancellationToken.None);
            _mongoDbCollectionMock.DeleteOneAsync(Arg.Any<FilterDefinition<TestEntity>>(), CancellationToken.None)
                .Returns(new DeleteResult.Acknowledged(1));

            _mongoDbCollectionMock.CountDocumentsAsync(Arg.Any<FilterDefinition<TestEntity>>(), Arg.Any<CountOptions>(),
                Arg.Any<CancellationToken>()).Returns(2);
            _mongoDbCollectionMock.FindAsync(
                Arg.Any<FilterDefinition<TestEntity>>(),
                Arg.Any<FindOptions<TestEntity>>(), CancellationToken.None).Returns(cursor);

            _mongoDbInstanceMock = Substitute.For<IMongoDbInstance>();
            _mongoDbInstanceMock.GetCollectionFor<TestEntity>()
                .Returns(_mongoDbCollectionMock);
        }

        [Test]
        public async Task FindByIdAsyncReturnsSingleResult()
        {
            var readRepository = new MongoRepository<TestEntity>(_mongoDbInstanceMock, _dateTimeProvider);

            var result = await readRepository.FindByIdAsync(_mockData[0].Id, CancellationToken.None);

            result.Should().NotBeNull().And.BeOfType<TestEntity>();
            result.Id.Should().NotBeEmpty();
            result.Id.Should().Be(_mockData[0].Id);
            result.Name.Should().Be(_mockData[0].Name);
            result.CreationDate.Should().Be(_mockData[0].CreationDate);

            _mongoDbInstanceMock.Received().GetCollectionFor<TestEntity>();
            await _mongoDbCollectionMock.Received().FindAsync(Arg.Any<FilterDefinition<TestEntity>>(),
                Arg.Any<FindOptions<TestEntity>>(), CancellationToken.None);
        }

        [Test]
        public async Task CountAsyncReturnsAmountOfEntries()
        {
            var readRepository = new MongoRepository<TestEntity>(_mongoDbInstanceMock, _dateTimeProvider);

            var result = await readRepository.CountAsync(QueryOptions<TestEntity>.Empty);

            result.Should().BeOfType(typeof(int));
            result.Should().Be(2);

            _mongoDbInstanceMock.Received().GetCollectionFor<TestEntity>();
            await _mongoDbCollectionMock.Received().CountDocumentsAsync(Arg.Any<FilterDefinition<TestEntity>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task FindAsyncWithoutFilterReturnsListResult()
        {
            var readRepository = new MongoRepository<TestEntity>(_mongoDbInstanceMock, _dateTimeProvider);

            var listResult = await readRepository.FindAsync(QueryOptions<TestEntity>.Empty);

            listResult.Should().NotBeNull().And.BeOfType<List<TestEntity>>();
            listResult.Should().HaveCount(2);
            listResult[0].Should().NotBeNull().And.BeOfType<TestEntity>();
            listResult[1].Should().NotBeNull().And.BeOfType<TestEntity>();

            for (var i = 0; i < _mockData.Count; i++)
            {
                listResult[i].Id.Should().Be(_mockData[i].Id);
                listResult[i].Id.Should().NotBeEmpty();
                listResult[i].Id.Should().Be(_mockData[i].Id);
                listResult[i].Name.Should().Be(_mockData[i].Name);
                listResult[i].CreationDate.Should().Be(_mockData[i].CreationDate);
            }

            _mongoDbInstanceMock.Received().GetCollectionFor<TestEntity>();
            await _mongoDbCollectionMock.FindAsync(Arg.Any<FilterDefinition<TestEntity>>(),
                Arg.Any<FindOptions<TestEntity>>(), CancellationToken.None);
        }

        [Test]
        public async Task FindAsyncWithFilterReturnsListResult()
        {
            var readRepository = new MongoRepository<TestEntity>(_mongoDbInstanceMock, _dateTimeProvider);

            var queryOptions = QueryOptions<TestEntity>.Parse("Name eq 'TestEntity-1'", "Name asc", 10, 0);
            var entities = await readRepository.FindAsync(queryOptions, CancellationToken.None);

            entities.Should().NotBeNull();
            _mongoDbInstanceMock.Received().GetCollectionFor<TestEntity>();
            await _mongoDbCollectionMock.FindAsync(Arg.Is<FilterDefinition<TestEntity>>(x => x != null),
                Arg.Is<FindOptions<TestEntity>>(s => s != null), CancellationToken.None);
        }

        [Test]
        public async Task AddAsyncReturnsCreatedEntity()
        {
            var writeRepository = new MongoRepository<TestEntity>(_mongoDbInstanceMock, _dateTimeProvider);
            var testEntity = _mockData[0];
            var addResult = await writeRepository.AddAsync(testEntity, CancellationToken.None);
            addResult.Should().NotBeNull().And.BeOfType<TestEntity>();
            addResult.Id.Should().NotBeEmpty();
            addResult.Id.Should().Be(_mockData[0].Id);
            addResult.Name.Should().Be(_mockData[0].Name);
            addResult.CreationDate.Should().Be(_mockData[0].CreationDate);

            _mongoDbInstanceMock.Received().GetCollectionFor<TestEntity>();
            await _mongoDbCollectionMock.Received().InsertOneAsync(Arg.Any<TestEntity>(),
                Arg.Any<InsertOneOptions>(), CancellationToken.None);
        }

        [Test]
        public async Task UpdateAsyncReturnsUpdatedEntity()
        {
            var writeRepository = new MongoRepository<TestEntity>(_mongoDbInstanceMock, _dateTimeProvider);
            var testEntity = new TestEntity(_mockData[1].Id,
                _mockData[0].Name,
                _mockData[0].CreationDate);
            var updateResult = await writeRepository.UpdateAsync(testEntity, CancellationToken.None);
            updateResult.Should().NotBeNull().And.BeOfType<TestEntity>();
            updateResult.Id.Should().NotBeEmpty();
            updateResult.Id.Should().Be(_mockData[1].Id);
            updateResult.Name.Should().Be(_mockData[0].Name);
            updateResult.CreationDate.Should().Be(_mockData[0].CreationDate);
            _mongoDbInstanceMock.Received().GetCollectionFor<TestEntity>();
            _mongoDbCollectionMock.ReceivedCalls().Any(x => x.GetMethodInfo().Name == nameof(IMongoCollection<TestEntity>.ReplaceOneAsync)).Should().BeTrue();
            // can't verify call to UpdateOneAsync because the CombinedUpdateDefinition type is internal and thus not usable with  Arg.Any<>
        }

        [Test]
        public async Task RemoveAsyncIsSuccessful()
        {
            var writeRepository = new MongoRepository<TestEntity>(_mongoDbInstanceMock, _dateTimeProvider);
            await writeRepository.RemoveAsync(
                _mockData[0].Id, CancellationToken.None);
            _mongoDbInstanceMock.Received().GetCollectionFor<TestEntity>();
            await _mongoDbCollectionMock.Received()
                .DeleteOneAsync(Arg.Any<FilterDefinition<TestEntity>>(), CancellationToken.None);
        }

        [Test]
        public async Task RemoveCanReturnAnError()
        {
            _mongoDbCollectionMock.DeleteOneAsync(Arg.Any<FilterDefinition<TestEntity>>(), CancellationToken.None)
                .Returns(DeleteResult.Unacknowledged.Instance);
            var writeRepository = new MongoRepository<TestEntity>(_mongoDbInstanceMock, _dateTimeProvider);
            await writeRepository.Invoking(async x => await x.RemoveAsync(
                _mockData[0].Id, CancellationToken.None)).Should().ThrowAsync<PiBoxException>();
            _mongoDbInstanceMock.Received().GetCollectionFor<TestEntity>();
        }
    }
}

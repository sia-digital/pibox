using System.Globalization;
using Chronos.Abstractions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Exceptions;
using PiBox.Plugins.Persistence.Abstractions;
using PiBox.Testing;

namespace PiBox.Plugins.Persistence.EntityFramework.Tests
{
    public class EntityFrameworkRepositoryTests
    {
        private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        private TestContext CreateContext(params TestEntity[] testEntities)
        {
            ActivityTestBootstrapper.Setup();
            _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
            var dbName = Guid.NewGuid().ToString("N");
            var dbOpts = new DbContextOptionsBuilder<TestContext>().UseInMemoryDatabase(dbName).Options;
            var testContext = new TestContext(dbOpts);
            if (!testEntities.Any()) return testContext;
            testContext.GetSet().AddRange(testEntities);
            testContext.GetContext().SaveChanges();
            return testContext;
        }

        private IRepository<TestEntity> GetRepository(IDbContext<TestEntity> dbContext) =>
            new EntityFrameworkRepository<TestEntity>(dbContext, _dateTimeProvider);

        [Test]
        public async Task FindByIdAsyncWorks()
        {
            var id = Guid.Parse("67243669-9318-4265-9270-f809b1ed1892");
            var entity = new TestEntity(id, "Tester", DateTime.UtcNow);
            await using var context = CreateContext(entity);
            var repository = GetRepository(context);
            var result = await repository.FindByIdAsync(id);
            result.Id.Should().Be(id);
            result.Name.Should().Be("Tester");
        }

        [Test]
        public async Task FindByIdAsyncCanReturnNotFound()
        {
            var id = Guid.Parse("67243669-9318-4265-9270-f809b1ed1892");
            await using var context = CreateContext();
            var repository = GetRepository(context);
            await repository.Invoking(async x => await x.FindByIdAsync(id))
                .Should().ThrowAsync<PiBoxException>();
        }

        [Test]
        public async Task FindAsyncWorksWithoutFilter()
        {
            await using var context = CreateContext(new TestEntity(Guid.NewGuid(), "Tester", DateTime.UtcNow), new TestEntity(Guid.NewGuid(), "Tester2", DateTime.UtcNow));
            var repository = GetRepository(context);
            var result = await repository.FindAsync(QueryOptions<TestEntity>.Empty);
            result.Should().HaveCount(2);
        }

        [Test]
        public async Task FindAsyncWorksWithFilter()
        {
            await using var context = CreateContext(new TestEntity(Guid.NewGuid(), "Tester", DateTime.UtcNow), new TestEntity(Guid.NewGuid(), "Tester2", DateTime.UtcNow));
            var repository = GetRepository(context);
            var filter = new QueryOptions<TestEntity>().WithFilter(x => x.Name == "Tester");
            var result = await repository.FindAsync(queryOptions: filter);
            result.Should().HaveCount(1);
            result[0].Name.Should().Be("Tester");
        }

        [Test]
        public async Task CountAsyncWorks()
        {
            await using var context = CreateContext(new TestEntity(Guid.NewGuid(), "Tester", DateTime.UtcNow), new TestEntity(Guid.NewGuid(), "Tester2", DateTime.UtcNow));
            var repository = GetRepository(context);
            var result = await repository.CountAsync(QueryOptions<TestEntity>.Empty);
            result.Should().Be(2);
        }

        [Test]
        public async Task CountAsyncWithFilterWorks()
        {
            await using var context = CreateContext(new TestEntity(Guid.NewGuid(), "Tester", DateTime.UtcNow), new TestEntity(Guid.NewGuid(), "Tester2", DateTime.UtcNow));
            var repository = GetRepository(context);
            var filter = new QueryOptions<TestEntity>().WithFilter(x => x.Name == "Tester");
            var result = await repository.CountAsync(filter);
            result.Should().Be(1);
        }

        [Test]
        public async Task AddAsyncWorks()
        {
            await using var context = CreateContext();
            var repository = GetRepository(context);
            var creationDate = DateTime.Parse("2020-01-01", CultureInfo.InvariantCulture);
            _dateTimeProvider.UtcNow.Returns(creationDate);
            var entity = new TestEntity(Guid.Empty, "Tester", DateTime.MinValue);
            var result = await repository.AddAsync(entity);
            entity = result;
            entity.Id.Should().NotBe(Guid.Empty);
            entity.Name.Should().Be("Tester");
            entity.CreationDate.Should().Be(creationDate);
        }

        [Test]
        public async Task UpdateAsyncWorks()
        {
            var creationDate = DateTime.Parse("2020-01-01", CultureInfo.InvariantCulture);
            var existingEntity = new TestEntity(Guid.NewGuid(), "Tester", creationDate);
            await using var context = CreateContext(existingEntity);
            var repository = GetRepository(context);
            var entity = new TestEntity(existingEntity.Id, "Tester2", DateTime.MinValue);
            var result = await repository.UpdateAsync(entity);
            entity = result;
            entity.Id.Should().Be(existingEntity.Id);
            entity.Name.Should().Be("Tester2");
            entity.CreationDate.Should().Be(creationDate);
        }

        [Test]
        public async Task UpdateAsyncCanReturnNotFound()
        {
            await using var context = CreateContext();
            var repository = GetRepository(context);
            var entity = new TestEntity(Guid.NewGuid(), "Tester", DateTime.MinValue);
            await repository.Invoking(async x => await x.UpdateAsync(entity)).Should().ThrowAsync<PiBoxException>();
        }

        [Test]
        public async Task RemoveAsyncWorks()
        {
            var existingEntity = new TestEntity(Guid.NewGuid(), "Tester", DateTime.UtcNow);
            await using var context = CreateContext(existingEntity);
            var repository = GetRepository(context);
            await repository.RemoveAsync(existingEntity.Id);
            var entities = await repository.FindAsync(QueryOptions<TestEntity>.Empty);
            entities.Should().HaveCount(0);
        }

        [Test]
        public async Task RemoveAsyncCanReturnNotFound()
        {
            await using var context = CreateContext();
            var repository = GetRepository(context);
            await repository.Invoking(async x => await x.RemoveAsync(Guid.NewGuid())).Should().ThrowAsync<PiBoxException>();
        }
    }
}

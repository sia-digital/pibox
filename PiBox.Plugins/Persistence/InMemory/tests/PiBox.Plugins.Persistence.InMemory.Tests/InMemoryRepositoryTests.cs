using Chronos.Abstractions;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Exceptions;
using PiBox.Plugins.Persistence.Abstractions;
using PiBox.Testing;

namespace PiBox.Plugins.Persistence.InMemory.Tests
{
    public class InMemoryRepositoryTests
    {
        public InMemoryRepositoryTests()
        {
            ActivityTestBootstrapper.Setup();
        }

        [Test]
        public async Task AddShouldWork()
        {
            var dateToSet = DateTime.UtcNow;
            var dateTimeProvider = Substitute.For<IDateTimeProvider>();
            dateTimeProvider.UtcNow.Returns(dateToSet);
            var repo = new InMemoryRepository<SampleModel>(dateTimeProvider, new InMemoryStore());
            var entity = await repo.AddAsync(new SampleModel { Name = "Test" });
            entity.Id.Should().NotBeEmpty();
            entity.Name.Should().Be("Test");
            entity.CreationDate.Should().Be(dateToSet);
        }

        [Test]
        public async Task UpdateShouldWork()
        {
            var id = Guid.Parse("bdbbfeb0-18fc-4b6b-b645-abd5dd98fdab");
            var creationDate = new DateTime(2022, 1, 1);
            var existingModel = new SampleModel
            {
                Id = id,
                Name = "Existing Model",
                CreationDate = creationDate
            };
            var inMemoryStore = new InMemoryStore();
            inMemoryStore.GetStore<SampleModel>().Add(existingModel);
            var dateTimeProvider = Substitute.For<IDateTimeProvider>();
            dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
            var repo = new InMemoryRepository<SampleModel>(dateTimeProvider, inMemoryStore);
            var newModel = new SampleModel
            {
                Id = id,
                Name = "My updated Model"
            };
            await repo.UpdateAsync(newModel);
            var sampleStore = inMemoryStore.GetStore<SampleModel>();
            sampleStore.Count.Should().Be(1);
            var entity = sampleStore.Single();
            entity.Id.Should().Be(id);
            entity.CreationDate.Should().Be(creationDate);
            entity.Name.Should().Be("My updated Model");
        }

        [Test]
        public async Task UpdateWontWorkIfTheEntityDoesNotExist()
        {
            var repo = new InMemoryRepository<SampleModel>(Substitute.For<IDateTimeProvider>(), new InMemoryStore());
            var id = Guid.Parse("24804b01-edea-4cfe-8c7c-b564f6dda563");
            var model = new SampleModel
            {
                Id = id,
                Name = "Test",
                CreationDate = DateTime.Now
            };
            await repo.Invoking(async x => await x.UpdateAsync(model)).Should().ThrowAsync<PiBoxException>();
        }

        [Test]
        public async Task RemoveShouldWork()
        {
            var id = Guid.Parse("bdbbfeb0-18fc-4b6b-b645-abd5dd98fdab");
            var creationDate = new DateTime(2022, 1, 1);
            var existingModel = new SampleModel
            {
                Id = id,
                Name = "Existing Model",
                CreationDate = creationDate
            };
            var inMemoryStore = new InMemoryStore();
            inMemoryStore.GetStore<SampleModel>().Add(existingModel);
            var dateTimeProvider = Substitute.For<IDateTimeProvider>();
            var repo = new InMemoryRepository<SampleModel>(dateTimeProvider, inMemoryStore);
            await repo.RemoveAsync(id);
            var sampleStore = inMemoryStore.GetStore<SampleModel>();
            sampleStore.Count.Should().Be(0);
        }

        [Test]
        public async Task RemoveWontWorkIfTheEntityDoesNotExist()
        {
            var repo = new InMemoryRepository<SampleModel>(Substitute.For<IDateTimeProvider>(), new InMemoryStore());
            var id = Guid.Parse("24804b01-edea-4cfe-8c7c-b564f6dda563");
            await repo.Invoking(async x => await x.RemoveAsync(id)).Should().ThrowAsync<PiBoxException>();
        }

        [Test]
        public async Task FindAsyncCanFilterEntries()
        {
            var store = new InMemoryStore();
            var sampleModelStore = store.GetStore<SampleModel>();
            var model1 = new SampleModel
            {
                Id = Guid.Parse("d61953f4-e9fc-4b8e-9f22-140ca0b05871"),
                Name = "test",
                CreationDate = new DateTime(2021, 1, 1)
            };
            sampleModelStore.Add(model1);
            var model2 = new SampleModel
            {
                Id = Guid.Parse("618fb4fd-6581-4063-98a0-d503ab7f4376"),
                Name = "sample",
                CreationDate = new DateTime(2021, 1, 1)
            };
            sampleModelStore.Add(model2);
            var repo = new InMemoryRepository<SampleModel>(Substitute.For<IDateTimeProvider>(), store);
            var queryOptions = new QueryOptions<SampleModel>().WithFilter("startswith(Name, 'sam')");
            var result = await repo.FindAsync(queryOptions);
            var entry = result.Single();
            entry.Should().NotBeNull();
            entry.Id.Should().Be(model2.Id);
        }

        [Test]
        public async Task CountAsyncWorks()
        {
            var store = new InMemoryStore();
            var sampleModelStore = store.GetStore<SampleModel>();
            var model1 = new SampleModel
            {
                Id = Guid.Parse("d61953f4-e9fc-4b8e-9f22-140ca0b05871"),
                Name = "test",
                CreationDate = new DateTime(2021, 1, 1)
            };
            sampleModelStore.Add(model1);
            var model2 = new SampleModel
            {
                Id = Guid.Parse("618fb4fd-6581-4063-98a0-d503ab7f4376"),
                Name = "sample",
                CreationDate = new DateTime(2021, 1, 1)
            };
            sampleModelStore.Add(model2);
            var repo = new InMemoryRepository<SampleModel>(Substitute.For<IDateTimeProvider>(), store);
            var result = await repo.CountAsync(QueryOptions<SampleModel>.Empty);
            result.Should().Be(2);
        }

        [Test]
        public async Task CountAsyncCanFilterEntries()
        {
            var store = new InMemoryStore();
            var sampleModelStore = store.GetStore<SampleModel>();
            var model1 = new SampleModel
            {
                Id = Guid.Parse("d61953f4-e9fc-4b8e-9f22-140ca0b05871"),
                Name = "test",
                CreationDate = new DateTime(2021, 1, 1)
            };
            sampleModelStore.Add(model1);
            var model2 = new SampleModel
            {
                Id = Guid.Parse("618fb4fd-6581-4063-98a0-d503ab7f4376"),
                Name = "sample",
                CreationDate = new DateTime(2021, 1, 1)
            };
            sampleModelStore.Add(model2);
            var repo = new InMemoryRepository<SampleModel>(Substitute.For<IDateTimeProvider>(), store);
            var queryOptions = new QueryOptions<SampleModel>().WithFilter("startswith(Name, 'sam')");
            var result = await repo.CountAsync(queryOptions);
            result.Should().Be(1);
        }

        [Test]
        public async Task FindByIdAsyncShouldWork()
        {
            var id = Guid.Parse("c522e9ed-bf3d-4e99-91d9-a3e05ae5d380");
            var store = new InMemoryStore();
            store.GetStore<SampleModel>().Add(new SampleModel { Id = id, Name = "test" });
            var repo = new InMemoryRepository<SampleModel>(Substitute.For<IDateTimeProvider>(), store);
            var model = await repo.FindByIdAsync(id);
            model.Id.Should().Be(id);
        }

        [Test]
        public async Task FindByIdAsyncWontWorkIfTheIdDoesNotExist()
        {
            var id = Guid.Parse("c522e9ed-bf3d-4e99-91d9-a3e05ae5d380");
            var repo = new InMemoryRepository<SampleModel>(Substitute.For<IDateTimeProvider>(), new InMemoryStore());
            await repo.Invoking(async x => await x.FindByIdAsync(id)).Should().ThrowAsync<PiBoxException>();
        }

        [Test]
        public async Task QueryByIdFilterWorks()
        {
            var repository = Substitute.For<IRepository<SampleModel>>();
            var emptyGuid = Guid.Empty;
            var query = new QueryOptions<SampleModel>().WithFilter(x => x.Id == emptyGuid);
            repository.CountAsync(query).Returns(1);
            var count = await repository.CountAsync(query);
            count.Should().Be(1);
        }
    }
}

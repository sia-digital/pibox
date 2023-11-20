using FluentAssertions;
using NUnit.Framework;
using PiBox.Testing.Models;

namespace PiBox.Plugins.Persistence.Abstractions.Tests
{
    public class QueryableExtensionsTests
    {
        private static readonly Guid EntityId1 = Guid.Parse("cb04ca44-0a4e-481b-a442-d72b3bcce1ea");
        private static readonly Guid EntityId2 = Guid.Parse("1c20943d-dedd-43cb-a3fc-727e2e53beed");
        private static readonly Guid EntityId3 = Guid.Parse("e81ffd56-4fcb-4337-ab02-3d73253dac7f");
        private static IQueryable<BaseTestEntity> Query => TestData.AsQueryable();
        private static readonly IList<BaseTestEntity> TestData = new List<BaseTestEntity>
        {
            new(EntityId1, "Test1", new DateTime(2000, 1, 1),
                new BaseSubTestEntity(Guid.Parse("eac4dac8-eeb3-42af-acb6-512f892ee682"), "SubTest1")),
            new(EntityId3, "Test3", new DateTime(2002, 1, 1),
                new BaseSubTestEntity(Guid.Parse("5831cc7c-1576-4507-9150-2d74dedd0abd"), "SubTest3")),
            new(EntityId2, "Test2", new DateTime(2001, 1, 1),
                new BaseSubTestEntity(Guid.Parse("3c433574-42f6-4681-b8d4-499684d651a1"), "SubTest2"))
        };

        [Test]
        public void CanFilterAQueryableWithQueryOptions()
        {
            var queryOptions = new QueryOptions<BaseTestEntity>().WithFilter("Name eq 'Test1' or (Name eq 'Test2' and year(CreationDate) eq 2001)");
            var result = Query.WithQueryOptions(queryOptions).ToList();
            result.Should().HaveCount(2);
            result.Should().Contain(f => f.Id == EntityId1);
            result.Should().Contain(f => f.Id == EntityId2);
        }

        [Test]
        public void CanFilterDeepMembers()
        {
            var queryOptions = new QueryOptions<BaseTestEntity>().WithFilter("endswith(subTestEntity/subNode, 'test2')");
            var result = Query.WithQueryOptions(queryOptions).ToList();
            result.Should().HaveCount(1);
            result.First().Id.Should().Be(EntityId2);
        }

        [Test]
        public void CanSortEntries()
        {
            var queryOptions = new QueryOptions<BaseTestEntity>().WithOrderBy("subTestEntity/subNode desc, Name asc");
            var result = Query.WithQueryOptions(queryOptions).ToList();
            result.Should().HaveCount(3);
            result[0].Id.Should().Be(EntityId3);
            result[1].Id.Should().Be(EntityId2);
            result[2].Id.Should().Be(EntityId1);

            queryOptions = new QueryOptions<BaseTestEntity>().WithOrderBy("subTestEntity/subNode asc, Name desc");
            result = Query.WithQueryOptions(queryOptions).ToList();
            result.Should().HaveCount(3);
            result[0].Id.Should().Be(EntityId1);
            result[1].Id.Should().Be(EntityId2);
            result[2].Id.Should().Be(EntityId3);
        }
    }
}

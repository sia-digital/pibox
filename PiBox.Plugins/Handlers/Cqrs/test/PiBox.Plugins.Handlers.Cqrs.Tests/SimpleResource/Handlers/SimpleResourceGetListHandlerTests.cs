using System.Linq.Expressions;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using ODataQueryHelper.Core.Model;
using PiBox.Extensions.Abstractions;
using PiBox.Hosting.Abstractions.DependencyInjection;
using PiBox.Hosting.Abstractions.Exceptions;
using PiBox.Plugins.Handlers.Abstractions.Cqrs.Handlers;
using PiBox.Plugins.Handlers.Cqrs.SimpleResource.Handlers;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Handlers.Cqrs.Tests.SimpleResource.Handlers
{
    public class SimpleResourceGetListHandlerTests
    {
        private IReadRepository<TestEntity> _repository = null!;
        private IGetListHandler<TestEntity> _handler = null!;

        [SetUp]
        public void Init()
        {
            _repository = Substitute.For<IReadRepository<TestEntity>>();
            var sp = Substitute.For<IServiceProvider>();
            sp.GetService(typeof(IReadRepository<TestEntity>)).Returns(_repository);
            var repoFactory = new Factory<IReadRepository<TestEntity>>(sp);
            _handler = new SimpleResourceGetListHandler<TestEntity>(repoFactory);
        }

        [Test]
        public async Task CanReturnValidationError()
        {
            var request = new PagingRequest(-1);
            await _handler.Invoking(async x => await x.GetListAsync(request, CancellationToken.None))
                .Should().ThrowAsync<ValidationPiBoxException>();
        }

        [Test]
        public async Task ReturnsErrorWhenTheRequestCanBeParsedAsQueryOptions()
        {
            var request = new PagingRequest(Filter: "NoProp eq 123");
            await _handler.Invoking(async x => await x.GetListAsync(request, CancellationToken.None))
                .Should().ThrowAsync<ValidationPiBoxException>();
        }

        [Test]
        public async Task GetListWorks()
        {
            var request = new PagingRequest(1, 0, "Name eq 'test1'", "Name asc");
            var entities = new TestEntity[] { new() { Id = Guid.NewGuid(), Name = "test1" } };
            Expression<Predicate<QueryOptions<TestEntity>>> queryOptsCompare = x => x.Page == 0
                                                                                    && x.Size == 1
                                                                                    && x.FilterExpression != null
                                                                                    && x.FilterExpression.ToString().Contains("Name")
                                                                                    && x.FilterExpression.ToString().Contains("test1")
                                                                                    && x.OrderByExpressions.Any()
                                                                                    && x.OrderByExpressions[0].Direction == OrderByDirectionType.Ascending
                                                                                    && x.OrderByExpressions[0].Expression.ToString().Contains("Name");
            _repository.CountAsync(Arg.Is(queryOptsCompare), Arg.Any<CancellationToken>())
                .Returns(1);
            _repository.FindAsync(Arg.Is(queryOptsCompare), Arg.Any<CancellationToken>())
                .Returns(entities.ToList());

            var result = await _handler.GetListAsync(request, CancellationToken.None);
            result.Page.TotalElements.Should().Be(1);
            result.Page.Current.Should().Be(0);
            result.Page.Size.Should().Be(1);
            result.Page.TotalPages.Should().Be(1);
            result.Items.Should().HaveCount(1);
            result.Items[0].Should().Be(entities[0]);
        }

        [Test]
        public async Task GetListFailsWhenCountDoesNotWork()
        {
            var request = new PagingRequest(1, 0, "Name eq 'test1'", "Name asc");
            var entities = new TestEntity[] { new() { Id = Guid.NewGuid(), Name = "test1" } };
            _repository.CountAsync(Arg.Any<QueryOptions<TestEntity>>(), Arg.Any<CancellationToken>())
                .Throws(new Exception());
            _repository.FindAsync(Arg.Any<QueryOptions<TestEntity>>(), Arg.Any<CancellationToken>())
                .Returns(entities.ToList());
            await _handler.Invoking(async x => await x.GetListAsync(request, CancellationToken.None))
                .Should().ThrowAsync<Exception>();
        }

        [Test]
        public async Task GetListFailsWhenFindDoesNotWork()
        {
            var request = new PagingRequest(1, 0, "Name eq 'test1'", "Name asc");
            _repository.CountAsync(Arg.Any<QueryOptions<TestEntity>>(), Arg.Any<CancellationToken>())
                .Returns(1);
            _repository.FindAsync(Arg.Any<QueryOptions<TestEntity>>(), Arg.Any<CancellationToken>())
                .Throws(new Exception());
            await _handler.Invoking(async x => await x.GetListAsync(request, CancellationToken.None))
                .Should().ThrowAsync<Exception>();
        }
    }
}

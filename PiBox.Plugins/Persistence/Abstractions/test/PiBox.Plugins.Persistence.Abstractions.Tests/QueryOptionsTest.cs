using System.Linq.Expressions;
using FluentAssertions;
using NUnit.Framework;
using ODataQueryHelper.Core.Model;
using PiBox.Hosting.Abstractions.Exceptions;
using PiBox.Plugins.Persistence.Abstractions.Exceptions;
using PiBox.Testing.Models;

namespace PiBox.Plugins.Persistence.Abstractions.Tests
{
    public class QueryOptionsTest
    {
        [Test]
        public void CanInitializeWithDefaultValues()
        {
            var queryOptions = new QueryOptions<BaseTestEntity>();
            queryOptions.FilterExpression.Should().BeNull();
            queryOptions.Page.Should().Be(PageNumber.Default);
            queryOptions.Size.Should().Be(PageSize.Default);
            queryOptions.OrderByExpressions.Should().BeEmpty();
        }

        [Test]
        public void CanCallMethodsWithNullValues()
        {
            var queryOptions = new QueryOptions<BaseTestEntity>();
            queryOptions.WithFilter((string)null);
            queryOptions.WithOrderBy(null);
            queryOptions.FilterExpression.Should().BeNull();
            queryOptions.OrderByExpressions.Should().BeEmpty();
        }

        [Test]
        public void CanHandleFilterStatement()
        {
            const string filterExpression = "Name eq 'Test'";
            var queryOptions = new QueryOptions<BaseTestEntity>().WithFilter(filterExpression);
            queryOptions.FilterExpression.Should().NotBeNull();
            var func = queryOptions.FilterExpression!.Compile();
            var baseEntity = new BaseTestEntity(Guid.Empty, "Test", DateTime.Now);
            func(baseEntity).Should().BeTrue();
        }

        private static string GetPropertyName(Expression<Func<BaseTestEntity, object>> selector)
        {
            if (selector.Body is MemberExpression memberExpression)
                return memberExpression.Member.Name;
            var nestedMemberExpression = (selector.Body as UnaryExpression)?.Operand as MemberExpression;
            return nestedMemberExpression?.Member.Name ?? "";
        }

        [Test]
        public void CanHandleOrderByStatement()
        {
            const string sortExpression = "name asc, id desc";
            var queryOptions = new QueryOptions<BaseTestEntity>().WithOrderBy(sortExpression);
            queryOptions.OrderByExpressions.Should().HaveCount(2);
            queryOptions.OrderByExpressions[0].Item1.Should().Be(OrderByDirectionType.Ascending);
            GetPropertyName(queryOptions.OrderByExpressions[0].Item2).Should().Be("Name");
            queryOptions.OrderByExpressions[1].Item1.Should().Be(OrderByDirectionType.Descending);
            GetPropertyName(queryOptions.OrderByExpressions[1].Item2).Should().Be("Id");
        }

        [Test]
        public void CanHandlePagingStatement()
        {
            var page = PageNumber.From(12);
            var size = PageSize.From(5);
            var queryOptions = new QueryOptions<BaseTestEntity>(page: page, size: size);
            queryOptions.Size.Should().Be(size);
            queryOptions.Page.Should().Be(page);
            queryOptions.WithPage(PageNumber.From(1));
            queryOptions.WithSize(PageSize.From(1));
            queryOptions.Size.Value.Should().Be(1);
            queryOptions.Page.Value.Should().Be(1);
        }

        [Test]
        public void EmptyIsDefaultAndOnlyAllocatedOnce()
        {
            var queryOptions = QueryOptions<BaseTestEntity>.Empty;
            queryOptions.Should().Be(QueryOptions<BaseTestEntity>.Empty);
            queryOptions.Page.Should().Be(PageNumber.Default);
            queryOptions.Size.Should().Be(PageSize.Default);
            queryOptions.FilterExpression.Should().BeNull();
            queryOptions.OrderByExpressions.Should().HaveCount(0);
        }

        [Test]
        public void QueryOptionsCanBeParsedSuccessfully()
        {
            const string sortExpression = "name asc, creationDate desc";
            const string filterExpression = "Name eq 'Test'";
            var val = new QueryOptions<BaseTestEntity>(PageSize.From(3), PageNumber.From(1)).WithFilter(filterExpression).WithOrderBy(sortExpression);
            val.FilterExpression.Should().NotBeNull();
            val.OrderByExpressions.Should().HaveCount(2);
            val.Page.Value.Should().Be(1);
            val.Size.Value.Should().Be(3);
        }

        [Test]
        public void QueryOptionsParseCanReturnAnError()
        {
            const string wrongExpression = "MySuperName eq 'Test'";
            var fn = () => QueryOptions<BaseTestEntity>.Parse(wrongExpression);
            fn.Invoking(x => x()).Should().Throw<ValidationPiBoxException>();
        }

        [Test]
        public void ToStringIsNiceToRead()
        {
            var queryOptions = new QueryOptions<BaseTestEntity>(PageSize.From(1), PageNumber.From(1)).WithFilter(x => x.Name == "Tester")
                .WithOrderBy(x => x.Name, OrderByDirectionType.Ascending);
            queryOptions.ToString().Should()
                .Be("Query(Page: 1, Size: 1, Filter: x => (x.Name == \"Tester\"), OrderBy: x => x.Name Ascending)");

            queryOptions = new QueryOptions<BaseTestEntity>(PageSize.From(1), PageNumber.From(1));
            queryOptions.ToString().Should()
                .Be("Query(Page: 1, Size: 1, Filter: null, OrderBy: null)");
        }

        [Test]
        public void ThrowsQueryOptionsExceptionOnInvalidExpressions()
        {
            var queryOptions = new QueryOptions<BaseTestEntity>();
            queryOptions.Invoking(x => x.WithFilter("NotExistingProp eq 123"))
                .Should().Throw<QueryOptionsException>("Unable to perform operation 'NotExistingProp'");

            queryOptions.Invoking(x => x.WithOrderBy("NotExistingProp asc"))
                .Should().Throw<QueryOptionsException>("Instance property 'NotExistingProp' is not defined for type 'PiBox.Testing.Models.BaseTestEntity' (Parameter 'propertyName')");
        }
    }
}

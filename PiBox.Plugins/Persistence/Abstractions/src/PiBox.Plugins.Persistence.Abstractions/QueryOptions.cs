using System.Linq.Expressions;
using ODataQueryHelper.Core;
using ODataQueryHelper.Core.Model;
using PiBox.Hosting.Abstractions.Exceptions;
using PiBox.Hosting.Abstractions.Middlewares.Models;
using PiBox.Plugins.Persistence.Abstractions.Exceptions;

namespace PiBox.Plugins.Persistence.Abstractions
{
    public class QueryOptions<T> where T : class
    {
        private static readonly ODataQueryParser<T> _queryParser = new();

        public QueryOptions() : this(PageSize.Default, PageNumber.Default) { }
        public QueryOptions(PageSize size, PageNumber page)
        {
            Size = size;
            Page = page;
        }

        public PageSize Size { get; private set; }
        public PageNumber Page { get; private set; }

        public Expression<Func<T, bool>> FilterExpression { get; private set; }

        public IList<(OrderByDirectionType Direction, Expression<Func<T, object>> Expression)> OrderByExpressions { get; } =
            new List<(OrderByDirectionType, Expression<Func<T, object>>)>();

        private QueryOptions<T> HandleExceptions(Action action)
        {
            try
            {
                action();
                return this;
            }
            catch (Exception e)
            {
                throw new QueryOptionsException(e.Message, e);
            }
        }

        public QueryOptions<T> WithPage(PageNumber page)
        {
            Page = page;
            return this;
        }

        public QueryOptions<T> WithSize(PageSize size)
        {
            Size = size;
            return this;
        }

        public QueryOptions<T> WithFilter(Expression<Func<T, bool>> predicate)
        {
            FilterExpression = FilterExpression is not null
                ? Expression.Lambda<Func<T, bool>>(Expression.AndAlso(FilterExpression, predicate))
                : predicate;
            return this;
        }

        public QueryOptions<T> WithFilter(string filter)
        {
            if (string.IsNullOrEmpty(filter)) return this;
            return HandleExceptions(() =>
            {
                var documentQuery = _queryParser.TryParse($"$filter={filter}");
                var predicate = documentQuery.Filter?.FilterExpression ?? throw new QueryOptionsException("Could not parse filter");
                WithFilter(predicate);
            });
        }

        public QueryOptions<T> WithOrderBy(Expression<Func<T, object>> propertySelector, OrderByDirectionType direction)
        {
            OrderByExpressions.Add((direction, propertySelector));
            return this;
        }

        public QueryOptions<T> WithOrderBy(string orderBy)
        {
            if (string.IsNullOrEmpty(orderBy)) return this;
            return HandleExceptions(() =>
            {
                var documentQuery = _queryParser.TryParse($"$orderby={orderBy}");
                var orderByExpressions = documentQuery.OrderBy?.OrderByNodes?.Select(x => (x.Direction, x.Expression))
                                         ?? ArraySegment<(OrderByDirectionType Direction, Expression<Func<T, object>> Expression)>.Empty;
                foreach (var (direction, expression) in orderByExpressions)
                    WithOrderBy(expression, direction);
            });
        }

        public override string ToString()
        {
            var filter = FilterExpression?.ToString() ?? "null";
            var orderBy = string.Join(", ", OrderByExpressions.Select(x => $"{x.Expression} {x.Direction}"));
            if (string.IsNullOrWhiteSpace(orderBy)) orderBy = "null";
            return $"Query(Page: {Page}, Size: {Size}, Filter: {filter}, OrderBy: {orderBy})";
        }

        public static readonly QueryOptions<T> Empty = new(PageSize.Default, PageNumber.Default);

        public static QueryOptions<T> Parse(string filter = null, string sort = null, int? size = null, int? page = null)
        {
            try
            {
                return new QueryOptions<T>(PageSize.FromNullable(size), PageNumber.FromNullable(page))
                    .WithFilter(filter)
                    .WithOrderBy(sort);
            }
            catch (QueryOptionsException)
            {
                throw new ValidationPiBoxException("Could not extract query options.", new List<FieldValidationError>());
            }
        }
    }
}

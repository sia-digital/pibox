using System.Linq.Expressions;
using ODataQueryHelper.Core.Model;

namespace PiBox.Plugins.Persistence.Abstractions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> WithQueryOptions<T>(this IQueryable<T> source, QueryOptions<T> queryOptions) where T : class
        {
            if (queryOptions.FilterExpression is not null)
                source = source.Where(queryOptions.FilterExpression!);
            return source.OrderByExpressions(queryOptions.OrderByExpressions).Skip(queryOptions.Size.Value * queryOptions.Page.Value).Take(queryOptions.Size.Value);
        }

        public static IQueryable<T> WithQueryOptionsForCount<T>(this IQueryable<T> source, QueryOptions<T> queryOptions) where T : class
        {
            return queryOptions.FilterExpression is not null ? source.Where(queryOptions.FilterExpression!) : source;
        }

        private static IQueryable<T> OrderByExpressions<T>(this IQueryable<T> source, IEnumerable<(OrderByDirectionType Direction, Expression<Func<T, object>> Expression)> orderByNodes)
        {
            return orderByNodes.Aggregate(source, (current, orderBy) => orderBy.Direction is OrderByDirectionType.Ascending
                ? current.SmartOrderBy(orderBy.Expression)
                : current.SmartOrderByDescending(orderBy.Expression));
        }

        private static bool IsOrdered<T>(this IQueryable<T> queryable)
        {
            return queryable.Expression.Type == typeof(IOrderedQueryable<T>);
        }

        private static IQueryable<T> SmartOrderBy<T, TKey>(this IQueryable<T> queryable, Expression<Func<T, TKey>> keySelector)
        {
            if (!queryable.IsOrdered()) return queryable.OrderBy(keySelector);
            var orderedQuery = queryable as IOrderedQueryable<T>;
            return orderedQuery!.ThenBy(keySelector);
        }

        private static IQueryable<T> SmartOrderByDescending<T, TKey>(this IQueryable<T> queryable, Expression<Func<T, TKey>> keySelector)
        {
            if (!queryable.IsOrdered()) return queryable.OrderByDescending(keySelector);
            var orderedQuery = queryable as IOrderedQueryable<T>;
            return orderedQuery!.ThenByDescending(keySelector);
        }
    }
}

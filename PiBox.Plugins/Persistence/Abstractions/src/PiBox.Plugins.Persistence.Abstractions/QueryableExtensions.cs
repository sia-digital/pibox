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

        private static IQueryable<T> OrderByExpressions<T>(this IQueryable<T> source, IList<(OrderByDirectionType Direction, Expression<Func<T, object>> Expression)> orderByNodes)
        {
            if (!orderByNodes.Any()) return source;
            var query = orderByNodes[0].Direction == OrderByDirectionType.Ascending
                ? source.OrderBy(orderByNodes[0].Expression) : source.OrderByDescending(orderByNodes[0].Expression);
            for (var i = 1; i < orderByNodes.Count; i++)
            {
                query = orderByNodes[i].Direction == OrderByDirectionType.Ascending
                    ? query.ThenBy(orderByNodes[i].Expression) : query.ThenByDescending(orderByNodes[i].Expression);
            }
            return query;
        }
    }
}

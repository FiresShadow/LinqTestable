using System.Linq;

namespace LinqTestable.Sources
{
    public static class ExpressionExtensions
    {
        public static IQueryable<T> ToTestable<T>(this IQueryable<T> query)
        {
            if (query is TestableQuery<T>)
                return query;

            return new TestableQuery<T>(query, new QueryChanger());
        }
    }
}
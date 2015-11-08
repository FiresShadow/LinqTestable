using System.Linq;

namespace LinqTestable.Sources.TestableQueryable
{
    public static class ExpressionExtensions
    {
        public static IQueryable<T> ToTestable<T>(this IQueryable<T> query)
        {
            if (query is TestableQueryable<T>)
                return query;

            return new TestableQueryable<T>(query, new MainQueryChanger());
        }
    }
}
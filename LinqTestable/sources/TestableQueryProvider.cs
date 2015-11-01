using System.Linq;
using System.Linq.Expressions;

namespace LinqTestable.Sources
{
    class TestableQueryProvider<T> : IQueryProvider
    {
        readonly IQueryable<T> _query;
        readonly IQueryChanger _queryChanger;

        internal TestableQueryProvider(IQueryable<T> query, IQueryChanger queryChanger)
        {
            _query = query;
            _queryChanger = queryChanger;
        }

        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
        {
            return new TestableQuery<TElement>(_query.Provider.CreateQuery<TElement>(_queryChanger.Change(expression)), _queryChanger);
        }

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            return _query.Provider.CreateQuery(_queryChanger.Change(expression));
        }

        TResult IQueryProvider.Execute<TResult>(Expression expression)
        {
            return _query.Provider.Execute<TResult>(_queryChanger.Change(expression));
        }

        object IQueryProvider.Execute(Expression expression)
        {
            return _query.Provider.Execute(_queryChanger.Change(expression));
        }
    }
}
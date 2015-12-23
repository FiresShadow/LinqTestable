using System.Linq;
using System.Linq.Expressions;

namespace LinqTestable.Sources
{
    class TestableQueryableProvider<T> : IQueryProvider
    {
        IQueryable<T> _query;
        readonly IQueryChanger _queryChanger;

        internal TestableQueryableProvider(IQueryable<T> query, IQueryChanger queryChanger)
        {
            _query = query;
            _queryChanger = queryChanger;
        }

        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
        {
            return new TestableQueryable<TElement>(_query.Provider.CreateQuery<TElement>(expression), _queryChanger);
        }

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            return _query.Provider.CreateQuery(expression);
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
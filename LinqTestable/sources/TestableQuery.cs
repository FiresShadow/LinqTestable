using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqTestable.Sources
{
    class TestableQuery<T> : IOrderedQueryable<T>
	{
		readonly TestableQueryProvider<T> _provider;
        readonly IQueryable<T> _query;

        internal TestableQuery (IQueryable<T> query, IQueryChanger queryChanger)
		{
			_query = query;
			_provider = new TestableQueryProvider<T> (query, queryChanger);
		}

        public Expression Expression { get { return _query.Expression; } }
		public Type ElementType { get { return typeof (T); } }
        public IQueryProvider Provider { get { return _provider; } }
        public IEnumerator<T> GetEnumerator () { return _query.GetEnumerator (); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        public override string ToString() { return _query.ToString(); }
	}
}
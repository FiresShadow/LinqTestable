using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;
using LinqTestable.Sources;

namespace LinqTestableTest
{
    public partial class MockObjectSet<T> : IObjectSet<T> where T : class
    {
        private readonly IList<T> _collection;

        public MockObjectSet(IList<T> collection)
        {
            _collection = collection;
        }

        public MockObjectSet()
        {
            _collection = new List<T>();
        }

        public void AddObject(T entity)
        {
            _collection.Add(entity);
        }

        public void Attach(T entity)
        {
            _collection.Add(entity);
        }

        public void DeleteObject(T entity)
        {
            _collection.Remove(entity);
        }

        public void Detach(T entity)
        {
            _collection.Remove(entity);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        public Type ElementType
        {
            get { return typeof(T); }
        }

        public System.Linq.Expressions.Expression Expression
        {
            get { return _collection.AsQueryable<T>().ToTestable().Expression; }
        }

        public IQueryProvider Provider
        {
            get { return _collection.AsQueryable<T>().ToTestable().Provider; }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;
using System.Linq.Expressions;
using LinqTestable.Sources;
using LinqTestable.Sources.TestableQueryable;

namespace LinqTestableTest
{
    class MockObjectSet<T> : IObjectSet<T> where T : class
    {
        private readonly IList<T> _collection;

        public MockObjectSet(TestDataModelSettings settings)
        {
            _collection = new List<T>();
            _settings = settings;
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

        public Expression Expression
        {
            get
            {
                return 
                    _settings.IsSmart ?
                    _collection.AsQueryable().ToTestable().Expression : //используйте эту строчку для подключения LinqTestable к пользовательскому проекту
                    _collection.AsQueryable().Expression;
            }
        }

        public IQueryProvider Provider
        {
            get { 
                    return
                        _settings.IsSmart ?
                        _collection.AsQueryable().ToTestable().Provider : //используйте эту строчку для подключения LinqTestable к пользовательскому проекту
                        _collection.AsQueryable().Provider;  
            }
        }

        private TestDataModelSettings _settings;
    }
}

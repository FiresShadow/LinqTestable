using System.Collections;
using System.Collections.Generic;
using System.Data.Objects;

namespace LinqTestableTest
{
    class CAR
    {
        public int CAR_ID { get; set; }
        public EntityCollection<DOOR> Doors { get; set; }
    }

    class DOOR
    {
        public int DOOR_ID { get; set; }
        public int CAR_ID { get; set; }
    }

    class DOOR_HANDLE
    {
        public int DOOR_HANDLE_ID { get; set; }
        public int DOOR_ID { get; set; }
        public string COLOR { get; set; }
        public int? MATERIAL_ID { get; set; }
        public int? MANUFACTURER_ID { get; set; }
    }

    interface IDataModel
    {
        IObjectSet<CAR> CAR { get; }
        IObjectSet<DOOR> DOOR { get; }
        IObjectSet<DOOR_HANDLE> DOOR_HANDLE { get; }
    }

    class TestDataModel : IDataModel
    {
        public IObjectSet<CAR> CAR { get; set; }
        public IObjectSet<DOOR> DOOR { get; set; }
        public IObjectSet<DOOR_HANDLE> DOOR_HANDLE { get; set; }

        public TestDataModel()
        {
            Settings = new TestDataModelSettings();
            CAR = new MockObjectSet<CAR>(Settings);
            DOOR = new MockObjectSet<DOOR>(Settings);
            DOOR_HANDLE = new MockObjectSet<DOOR_HANDLE>(Settings);
        }

        public TestDataModelSettings Settings { get; set; }
    }

    class TestDataModelSettings
    {
        /// <summary>
        /// Нужно ли устранять концептуальный разрыв между реляционной и объектной моделью
        /// </summary>
        public bool IsSmart { get; set; }
    }

    class EntityCollection<TEntity> : IEnumerable<TEntity> where TEntity : class
    {
        private readonly IEnumerable<TEntity> _collection;

        public EntityCollection(IEnumerable<TEntity> collection)
        {
            _collection = collection;
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _collection.GetEnumerator();
        }
    }
}
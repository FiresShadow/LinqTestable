using System.Data.Objects;

namespace LinqTestableTest
{
    public class CAR
    {
        public int CAR_ID { get; set; } 
    }

    public class DOOR
    {
        public int DOOR_ID { get; set; }
        public int CAR_ID { get; set; }
    }

    public class DOOR_HANDLE
    {
        public int DOOR_HANDLE_ID { get; set; }
        public int DOOR_ID { get; set; }
    }

    public interface IDataModel
    {
        IObjectSet<CAR> CAR { get; set; }
        IObjectSet<DOOR> DOOR { get; set; }
        IObjectSet<DOOR_HANDLE> DOOR_HANDLE { get; set; }
    }

    public class TestDataModel : IDataModel
    {
        public IObjectSet<CAR> CAR
        {
            get { return _car; }
            set { _car = value; }
        }

        public IObjectSet<DOOR> DOOR
        {
            get { return _door; }
            set { _door = value; }
        }

        public IObjectSet<DOOR_HANDLE> DOOR_HANDLE
        {
            get { return _doorHandle; }
            set { _doorHandle = value; }
        }

        private IObjectSet<CAR> _car = new MockObjectSet<CAR>();
        private IObjectSet<DOOR> _door = new MockObjectSet<DOOR>();
        private IObjectSet<DOOR_HANDLE> _doorHandle = new MockObjectSet<DOOR_HANDLE>();
    }
}
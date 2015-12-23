using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LinqTestableTest.Tests
{
    [TestFixture]
    //Произвольные запросы, просто чтобы протестировать корректность обхода дерева выражений
    public class InternalTests
    {
        [Test]
        public void Test()
        {
            var dataModel = new TestDataModel { Settings = { IsSmart = true } };
            var door = new DOOR { CAR_ID = 1, DOOR_ID = 100};
            dataModel.DOOR.AddObject(door);
            dataModel.CAR.AddObject(new CAR {CAR_ID = 1, Doors = new EntityCollection<DOOR>(new []{door})});

            var carByDoor = dataModel.CAR.Where(car => car.Doors.Any(d => d.DOOR_ID == 100)).FirstOrDefault();

            Assert.NotNull(carByDoor);
        }

        [Test]
        public void Test2()
        {
            var dataModel = new TestDataModel { Settings = { IsSmart = true }};
            dataModel.CAR.AddObject(new CAR {CAR_ID = 1} );
            dataModel.CAR.AddObject(new CAR {CAR_ID = 2} );

            var cars =
                (from car in dataModel.CAR
                select new {car.CAR_ID, Container = new Container{Id = car.CAR_ID, Id2 = car.CAR_ID + 1, SubContainer = new Container{Id = 5, SubContainer = new Container()}}}
                )
                .Where(x => x.Container.Id2 != 3)
                .ToList();

            var carExtended = cars.Single();

            Assert.AreEqual(1, carExtended.CAR_ID);
            Assert.AreEqual(1, carExtended.Container.Id);
            Assert.AreEqual(2, carExtended.Container.Id2);
            Assert.AreEqual(5, carExtended.Container.SubContainer.Id);
        }

        [Test]
        public void Test3()
        {
            var dataModel = new TestDataModel { Settings = { IsSmart = true } };
            dataModel.DOOR_HANDLE.AddObject(new DOOR_HANDLE {DOOR_HANDLE_ID = 1, MATERIAL_ID = 1});

            var doorHandleIds = new List<int> { 1, 2 };

            var doorHandles =
                (from doorHandle in dataModel.DOOR_HANDLE
                 where doorHandleIds.Select(x => x).Select(x => x).Any()
                 select doorHandle).ToList();

            Assert.AreEqual(1, doorHandles.Count);
        }

        [Test]
        public void Test4()
        {
            var dataModel = new TestDataModel {Settings = {IsSmart = true}};
            dataModel.CAR.Where(y => y.CAR_ID == dataModel.CAR.Select(x => x).Sum(x => x.CAR_ID)).ToList();
        }

        [Test]
        public void Test5()
        {
            var dataModel = new TestDataModel { Settings = { IsSmart = true } };

            dataModel.CAR.AddObject(new CAR { CAR_ID = 1 });
            dataModel.CAR.AddObject(new CAR { CAR_ID = 2 });
            dataModel.DOOR.AddObject(new DOOR { CAR_ID = 1, DOOR_ID = 1 });
            dataModel.DOOR.AddObject(new DOOR { CAR_ID = 2, DOOR_ID = 2 });
            dataModel.DOOR_HANDLE.AddObject(new DOOR_HANDLE { DOOR_ID = 1, DOOR_HANDLE_ID = 1, COLOR = "RED" });

            var carsWithoutRedHandle =
                   (from car in dataModel.CAR
                    join door in dataModel.DOOR on car.CAR_ID equals door.CAR_ID
                            into joinedDoor
                    from door in joinedDoor.DefaultIfEmpty()
                    join doorHandle in dataModel.DOOR_HANDLE on new { door.DOOR_ID } equals new { doorHandle.DOOR_ID }
                            into joinedDoorHandle
                    from doorHandle in joinedDoorHandle.DefaultIfEmpty()
                    where doorHandle.COLOR != "RED" || doorHandle == null
                    select new{car}).First();

            Assert.AreEqual(2, carsWithoutRedHandle.car.CAR_ID);
        }

        [Test]
        public void Test6()
        {
            var dataModel = new TestDataModel { Settings = { IsSmart = true } };

            dataModel.CAR.AddObject(new CAR { CAR_ID = 1 });
            dataModel.CAR.AddObject(new CAR { CAR_ID = 2 });
            dataModel.DOOR.AddObject(new DOOR { CAR_ID = 1, DOOR_ID = 1 });
            dataModel.DOOR.AddObject(new DOOR { CAR_ID = 2, DOOR_ID = 2 });
            dataModel.DOOR_HANDLE.AddObject(new DOOR_HANDLE { DOOR_ID = 1, DOOR_HANDLE_ID = 1, COLOR = "RED" });

            var carsWithoutRedHandle =
                   (from car in dataModel.CAR
                    join door in dataModel.DOOR on car.CAR_ID equals door.CAR_ID
                            into joinedDoor
                    from door in joinedDoor.DefaultIfEmpty()
                    join doorHandle in dataModel.DOOR_HANDLE on new { door.DOOR_ID } equals new { doorHandle.DOOR_ID }
                            into joinedDoorHandle
                    from doorHandle in joinedDoorHandle.DefaultIfEmpty()
                    where doorHandle.COLOR != "RED" || doorHandle == null
                    select car).First();

            Assert.AreEqual(2, carsWithoutRedHandle.CAR_ID);
        }

        [Test]
        public void Test7()
        {
            var dataModel = new TestDataModel { Settings = { IsSmart = true } };
            dataModel.CAR.AddObject(new CAR { CAR_ID = 1 });
            dataModel.CAR.AddObject(new CAR { CAR_ID = 2 });

            var ids =
                (from car in dataModel.CAR
                 select new { Id = car.CAR_ID > 1 ? car.CAR_ID : -1 }
                )
                .ToList();


            Assert.AreEqual(2, ids.Count);
            Assert.AreEqual(-1, ids[0].Id);
            Assert.AreEqual(2, ids[1].Id);
        }

        [Test]
        public void Test8()
        {
            var dataModel = new TestDataModel { Settings = { IsSmart = true } };
            dataModel.DOOR.AddObject(new DOOR { DOOR_ID = 100 });
            dataModel.DOOR_HANDLE.AddObject(new DOOR_HANDLE {MATERIAL_ID = 100});
            dataModel.DOOR_HANDLE.AddObject(new DOOR_HANDLE());

            var handles =
                (from handle in dataModel.DOOR_HANDLE
                select new {Id = handle.MATERIAL_ID.Value});

            var doors =
                from door in dataModel.DOOR
                where handles.Any(handle => handle.Id == door.DOOR_ID) 
                select door;

            Assert.AreEqual(1, doors.Count());
        }

        [Test]
        public void Test9()
        {
            var dataModel = new TestDataModel { Settings = { IsSmart = true } };
            dataModel.CAR.AddObject(new CAR { CAR_ID = 1 });
            dataModel.DOOR.AddObject(new DOOR { CAR_ID = 1, DOOR_ID = 10 });

            var cars =
                (from car in dataModel.CAR
                select new { CAR_ID = (int?)car.CAR_ID });

            cars = cars.Union(new[] {new {CAR_ID = (int?)null}});

            var doors =
                from door in dataModel.DOOR
                select new{ DoorId = door.DOOR_ID, Cars = cars.Where( car => car.CAR_ID == door.CAR_ID )};

            var finalDoors =
                (from door in dataModel.DOOR
                where doors.Any(d => d.Cars.Any(c => c.CAR_ID == door.CAR_ID))
                select door).ToList();

            Assert.AreEqual(1, finalDoors.Count);
        }

        [Test]
        public void Test10()
        {
            var dataModel = new TestDataModel { Settings = { IsSmart = true } };
            dataModel.CAR.AddObject(new CAR { CAR_ID = 1 });
            dataModel.DOOR.AddObject(new DOOR { CAR_ID = 1, DOOR_ID = 10 });

            var finalDoors =
                (from door in dataModel.DOOR
                 where (from door1 in dataModel.DOOR
                     select new { DoorId = door1.DOOR_ID, Cars = (from car in dataModel.CAR
                         select new { CAR_ID = (int?)car.CAR_ID }).Union(new[] { new { CAR_ID = (int?)null } }).Where(car => car.CAR_ID == door1.CAR_ID) }).Any(d => d.Cars.Any(c => c.CAR_ID == door.CAR_ID))
                 select door).ToList();

            Assert.AreEqual(1, finalDoors.Count);
        }

        public class Container
        {
            public int Id { get; set; }
            public int Id2 { get; set; }
            public Container SubContainer { get; set; }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqTestable.Sources;
using NUnit.Framework;

namespace LinqTestableTest
{
    [TestFixture]
    public class ToTestableTest
    {
        [Test]
        public void TwoLeftJoinsShouldNotThrow()
        {
            var dataModel = new TestDataModel();

            const int carId = 100;
            dataModel.CAR = new MockObjectSet<CAR>(new List<CAR>{new CAR{CAR_ID = carId}});

            var cars =
                (from car in dataModel.CAR
                join door in dataModel.DOOR on car.CAR_ID equals door.CAR_ID into joinedDoor from door in joinedDoor.DefaultIfEmpty()
                join doorHandle in dataModel.DOOR_HANDLE on door.DOOR_ID equals doorHandle.DOOR_ID into joinedDoorHandle from doorHandle in joinedDoorHandle.DefaultIfEmpty()
                select car).ToList();

            Assert.AreEqual(1, cars.Count);
            Assert.AreEqual(carId, cars.First().CAR_ID);
        }
    }
}
using System;
using System.Linq;
using NUnit.Framework;

namespace LinqTestableTest.Tests
{
    [TestFixture]
    public class TwoLeftJoins
    {
        void ExecuteTwoLeftJoins(bool isSmart)
        {
            var dataModel = new TestDataModel {Settings = {IsSmart = isSmart}};

            const int carId = 100;
            dataModel.CAR.AddObject(new CAR{CAR_ID = carId});
            dataModel.CAR.AddObject(new CAR{CAR_ID = carId + 1});

            var cars =
                (from car in dataModel.CAR
                join door in dataModel.DOOR on car.CAR_ID equals door.CAR_ID 
                    into joinedDoor from door in joinedDoor.DefaultIfEmpty()
                join doorHandle in dataModel.DOOR_HANDLE on door.DOOR_ID equals doorHandle.DOOR_ID 
                    into joinedDoorHandle from doorHandle in joinedDoorHandle.DefaultIfEmpty()
                select car).ToList();

            Assert.AreEqual(2, cars.Count);
            Assert.AreEqual(carId, cars.First().CAR_ID);
        }

        [Test]
        public void TwoLeftJoinsShouldThrow()
        {
            Assert.Throws<NullReferenceException>(() => ExecuteTwoLeftJoins(false));
        }

        [Test]
        public void SmartTwoLeftJoinsShouldNotThrow()
        {
            ExecuteTwoLeftJoins(true);
        }
    }
}
﻿using System;
using System.Linq;
using NUnit.Framework;

namespace LinqTestableTest.Tests
{
    [TestFixture]
    public class Select
    {
        void ExecuteSelect(bool isSmart)
        {
            var dataModel = new TestDataModel { Settings = { IsSmart = isSmart } };

            dataModel.CAR.AddObject(new CAR {CAR_ID = 1});
            dataModel.CAR.AddObject(new CAR {CAR_ID = 2});
            dataModel.DOOR.AddObject(new DOOR {CAR_ID = 1, DOOR_ID = 1});
            dataModel.DOOR.AddObject(new DOOR {CAR_ID = 2, DOOR_ID = 2});
            dataModel.DOOR_HANDLE.AddObject(new DOOR_HANDLE{DOOR_ID = 1, DOOR_HANDLE_ID = 1, COLOR = "RED"});

            var carsWithoutRedHandle =
                   (from car in dataModel.CAR
                    join door in dataModel.DOOR on car.CAR_ID equals door.CAR_ID 
                            into joinedDoor from door in joinedDoor.DefaultIfEmpty()
                    join doorHandle in dataModel.DOOR_HANDLE on new {door.DOOR_ID} equals new {doorHandle.DOOR_ID} 
                            into joinedDoorHandle from doorHandle in joinedDoorHandle.DefaultIfEmpty()
                    where doorHandle.COLOR != "RED" || doorHandle == null
                    select car).ToList();

            Assert.AreEqual(1, carsWithoutRedHandle.Count);
            Assert.AreEqual(2, carsWithoutRedHandle.First().CAR_ID);
        }

        [Test]
        public void SelectShouldThrow()
        {
            Assert.Throws<NullReferenceException>(() => ExecuteSelect(false));
        }

        [Test]
        public void SmartSelectShouldNotThrow()
        {
            ExecuteSelect(true);
        }
    }
}
using System;
using System.Linq;
using NUnit.Framework;

namespace LinqTestableTest.Tests
{
    [TestFixture]
    public class GroupBy
    {
        private void ExecuteGroupBy(bool isSmart)
        {
            var dataModel = new TestDataModel {Settings = {IsSmart = isSmart}};

            dataModel.DOOR.AddObject(new DOOR {DOOR_ID = 1});
            dataModel.DOOR.AddObject(new DOOR { DOOR_ID = 2 });
            dataModel.DOOR_HANDLE.AddObject(new DOOR_HANDLE { DOOR_HANDLE_ID = 1, DOOR_ID = 1, COLOR = "YELLOW" });

            var minIdDoors =
                (from door in dataModel.DOOR
                 join doorHandle in dataModel.DOOR_HANDLE on door.DOOR_ID equals doorHandle.DOOR_ID into joinedDoorHandle from doorHandle in joinedDoorHandle.DefaultIfEmpty()
                 join doorHandle2 in dataModel.DOOR_HANDLE on doorHandle.DOOR_HANDLE_ID equals doorHandle2.DOOR_HANDLE_ID into joinedDoorHandle2 from doorHandle2 in joinedDoorHandle2.DefaultIfEmpty()
                 group new {doorHandle2, door} by doorHandle2.COLOR into groupedDoorHandle
                 select new {Id = groupedDoorHandle.Min(x => x.door.DOOR_ID), Color = groupedDoorHandle.Key }).ToList();

            Assert.AreEqual(1, minIdDoors.First().Id);
            Assert.AreEqual("YELLOW", minIdDoors.First().Color);
            Assert.AreEqual(null, minIdDoors[1].Color);
        }

        [Test]
        public void GroupJoinShouldFail()
        {
            Assert.Throws<NullReferenceException>(() => ExecuteGroupBy(false));
        }

        [Test]
        public void SmartGroupJoinShouldSuccess()
        {
            ExecuteGroupBy(true);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using LinqTestable.Sources.Infrastructure;
using NUnit.Framework;

namespace LinqTestableTest.Tests
{
    [TestFixture]
    public class Contains
    {
        public void ExecuteContains(bool isSmart)
        {
            var dataModel = new TestDataModel { Settings = { IsSmart = isSmart } };

            new[]
            {
                new DOOR_HANDLE {DOOR_HANDLE_ID = 1, MATERIAL_ID = 1},
                new DOOR_HANDLE {DOOR_HANDLE_ID = 2, MATERIAL_ID = 2},
                new DOOR_HANDLE {DOOR_HANDLE_ID = 3}
            }
                .ForEach(dataModel.DOOR_HANDLE.AddObject);
           
            var doorHandleIds = new List<int>{1,2};

            var doorHandles =
                (from doorHandle in dataModel.DOOR_HANDLE
                where doorHandleIds.Contains(doorHandle.MATERIAL_ID.Value)
                select doorHandle).ToList();

            Assert.AreEqual(2, doorHandles.Count);
        }

        [Test]
        public void ContainsShouldFail()
        {
            Assert.Throws<InvalidOperationException>(() => ExecuteContains(false));
        }

        [Test]
        public void SmartContainsShouldSuccess()
        {
            ExecuteContains(true);
        }
    }
}
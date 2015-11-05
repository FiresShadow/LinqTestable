using System.Linq;
using LinqTestable.Sources;
using LinqTestable.Sources.Infrastructure;
using NUnit.Framework;

namespace LinqTestableTest.Tests
{
    [TestFixture]
    public class NullComparison
    {
        void ExecuteNullComparison(bool isSmart)
        {
            var dataModel = new TestDataModel { Settings = { IsSmart = isSmart } };

            new[]
            {
                new DOOR_HANDLE {DOOR_HANDLE_ID = 1, MATERIAL_ID = 1, MANUFACTURER_ID = 1}, // <----
                new DOOR_HANDLE {DOOR_HANDLE_ID = 2, MATERIAL_ID = 2, MANUFACTURER_ID = 2}, //      |-- this is only pair
                new DOOR_HANDLE {DOOR_HANDLE_ID = 3, MATERIAL_ID = 1, MANUFACTURER_ID = 1}, // <----
                new DOOR_HANDLE {DOOR_HANDLE_ID = 4, MATERIAL_ID = 5, MANUFACTURER_ID = null},
                new DOOR_HANDLE {DOOR_HANDLE_ID = 5, MATERIAL_ID = 5, MANUFACTURER_ID = null},
                new DOOR_HANDLE {DOOR_HANDLE_ID = 6, MATERIAL_ID = null, MANUFACTURER_ID = null},
                new DOOR_HANDLE {DOOR_HANDLE_ID = 7, MATERIAL_ID = null, MANUFACTURER_ID = null}
            }
            .ForEach(x => dataModel.DOOR_HANDLE.AddObject(x));

            var handlePairsWithSameMaterialAndManufacturer =
               (from handle in dataModel.DOOR_HANDLE
                join anotherHandle in dataModel.DOOR_HANDLE on handle.MATERIAL_ID equals anotherHandle.MATERIAL_ID
                where handle.MANUFACTURER_ID == anotherHandle.MANUFACTURER_ID && handle.DOOR_HANDLE_ID < anotherHandle.DOOR_HANDLE_ID
                select new {handle, anotherHandle}).ToList();

            Assert.AreEqual(1, handlePairsWithSameMaterialAndManufacturer.Count);
            var pair = handlePairsWithSameMaterialAndManufacturer.First();
            Assert.AreEqual(1, pair.handle.MATERIAL_ID);
            Assert.AreEqual(pair.handle.MATERIAL_ID, pair.anotherHandle.MATERIAL_ID);
            Assert.AreEqual(1, pair.handle.MANUFACTURER_ID);
            Assert.AreEqual(pair.handle.MANUFACTURER_ID, pair.anotherHandle.MANUFACTURER_ID);
        }

        [Test]
        public void NullComparisonShouldFail()
        {
            Assert.Throws<AssertionException>(() => ExecuteNullComparison(false));
        }

        [Ignore("Not realized yet")]
        [Test]
        public void SmartNullComparisonShouldSuccess()
        {
            ExecuteNullComparison(true);
        }
    }
}
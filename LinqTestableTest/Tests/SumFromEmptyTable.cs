using System;
using System.Linq;
using NUnit.Framework;

namespace LinqTestableTest.Tests
{
    [TestFixture]
    public class SumFromEmptyTable
    {
        void ExecuteSumFromEmptyTable(bool isSmart)
        {
            var dataModel = new TestDataModel {Settings = {IsSmart = isSmart}};
            int sum = dataModel.CAR.Sum(x => x.CAR_ID);
        }

        [Test]
        public void SmartSumShouldThrow()
        {
            Assert.Throws<InvalidOperationException>(() => ExecuteSumFromEmptyTable(true));
        }

        [Test]
        public void SumShouldNotThrow()
        {
            ExecuteSumFromEmptyTable(false);
        }

        void ExecuteNullableSumFromEmptyTable(bool isSmart)
        {
            var dataModel = new TestDataModel { Settings = { IsSmart = isSmart } };
            int? sum = dataModel.DOOR_HANDLE.Sum(x => x.MATERIAL_ID);
            Assert.AreEqual(null, sum);
        }

        [Test]
        public void NullableSumShouldFail()
        {
            Assert.Throws<AssertionException>(() => ExecuteNullableSumFromEmptyTable(false));
        }

        [Test]
        public void NullableSmartSumShouldSuccess()
        {
            ExecuteNullableSumFromEmptyTable(true);
        }
    }
}
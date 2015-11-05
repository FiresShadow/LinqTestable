using System;
using System.Linq;
using System.Reflection;
using LinqTestable.Sources.Infrastructure;
using NUnit.Framework;

namespace LinqTestableTest.Tests
{
    [TestFixture]
    public class CouldNotMaterializeException
    {
        [Test]
        public void WhenCouldNotMaterializeShouldThrow()
        {
            var dataModel = new TestDataModel {Settings = {IsSmart = true}};

            dataModel.CAR.AddObject(new CAR{CAR_ID = 1});

            var propertyType = typeof(DOOR).GetProperty(NameSelecter.GetMemberName<DOOR>(x => x.DOOR_ID)).PropertyType;
            Assert.False(propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>));

            var cars =
                   (from car in dataModel.CAR
                    join door in dataModel.DOOR on car.CAR_ID equals door.CAR_ID
                        into joinedDoor from door in joinedDoor.DefaultIfEmpty()
                    select new { car.CAR_ID, door.DOOR_ID });

            Assert.Throws<NullReferenceException>(() => cars.ToList()); //ORM couldn't materialize null to not-nullable door.DOOR_ID and throw an exception, so unit-test should throw too.
        }
    }
}
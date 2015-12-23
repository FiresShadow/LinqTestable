using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqTestable.Sources.Infrastructure
{
    class CompressedObject : Dictionary<string, object>
    {
        protected bool Equals(CompressedObject other)
        {
            return this.All(keyValue => other[keyValue.Key].Equals(keyValue.Value));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CompressedObject) obj);
        }

        public override int GetHashCode()
        {
            int hashCode = 0;

            unchecked
            {
                hashCode = this.Aggregate(hashCode, (current, keyValue) => current ^ keyValue.Value.GetHashCode());
            }

            return hashCode;
        }

        public static bool operator ==(CompressedObject value1, CompressedObject value2)
        {
            if (ReferenceEquals(value1, null))
            {
                return ReferenceEquals(value2, null);
            }

            return value1.Equals(value2);
        }

        public static bool operator !=(CompressedObject value1, CompressedObject value2)
        {
            return !(value1 == value2);
        }

        public object GetValueByNumber(int number)
        {
            return this.ToList()[number].Value;
        }

        public object GetItem(string name)
        {
            return this[name];
        }
    }
}
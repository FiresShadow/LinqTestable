using System.Collections.Generic;
using System.Reflection;

namespace LinqTestable.Sources
{
    public static class LinqTestableSettings
    {
        private static Dictionary<MethodInfo, MethodInfo> _nullableSynonims = new Dictionary<MethodInfo, MethodInfo>();
        
        public static Dictionary<MethodInfo, MethodInfo> NullableSynonims
        {
            get { return _nullableSynonims; }
            set
            {
                _nullableSynonims = value;

                if (value == null)
                    _nullableSynonims = new Dictionary<MethodInfo, MethodInfo>();
            }
        }
    }
}
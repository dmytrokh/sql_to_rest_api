using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.Serialization;

namespace SqlToRestApi.Models
{
    [Serializable]
    public class DynamicContext : DynamicObject, ISerializable
    {
        private readonly Dictionary<string, object> _dynamicContext = new Dictionary<string, object>();

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return (_dynamicContext.TryGetValue(binder.Name, out result));
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _dynamicContext.Add(binder.Name, value);
            return true;
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            foreach (var kvp in _dynamicContext)
            {
                info.AddValue(kvp.Key, kvp.Value);
            }
        }

        public DynamicContext()
        {
        }

        protected DynamicContext(SerializationInfo info, StreamingContext context)
        {
            // TODO: validate inputs before deserializing. See http://msdn.microsoft.com/en-us/library/ty01x675(VS.80).aspx
            foreach (var entry in info)
            {
                _dynamicContext.Add(entry.Name, entry.Value);
            }
        }
    }
}
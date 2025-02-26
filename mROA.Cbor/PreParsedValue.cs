using System;
using System.Collections.Generic;
using mROA.Abstract;
using mROA.Implementation;

namespace mROA.Cbor
{
    public class PreParsedValue
    {
        public List<object> Properties { get; set; }

        public PreParsedValue(List<object> properties)
        {
            Properties = properties;
        }

        public object? ToObject(Type type, IEndPointContext? context)
        {
            var instance = Activator.CreateInstance(type);
            if (instance == null)
                return null;

            if (instance is ISharedObject sharedObject && context != null)
            {
                sharedObject.EndPointContext = context;
            }
            
            var properties = CborSerializationToolkit.FilterProperties(type.GetProperties());
            for (var index = 0; index < properties.Count; index++)
            {
                var property = properties[index];
                property.SetValue(instance, Properties[index]);
            }
            return instance;
        }
    }
}
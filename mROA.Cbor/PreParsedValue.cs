using System;
using System.Collections.Generic;
using mROA.Abstract;
using mROA.Implementation;

namespace mROA.Cbor
{
    public interface IPreParsedValue
    {
        object? ToObject(Type type, IEndPointContext? context);
    }

    public class PreParsedValue : IPreParsedValue
    {
        private List<object> _properties { get; set; }

        public PreParsedValue(List<object> properties)
        {
            _properties = properties;
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
                property.SetValue(instance, _properties[index] is IPreParsedValue ppv ? ppv.ToObject(property.PropertyType, context) : _properties[index]);
            }

            return instance;
        }
    }

    public class ParsedValue : IPreParsedValue
    {
        public ParsedValue(object? value)
        {
            _value = value;
        }

        private object? _value;


        public object? ToObject(Type type, IEndPointContext? context)
        {
            return _value;
        }
    }
}
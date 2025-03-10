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
        public PreParsedValue(List<object> properties)
        {
            _properties = properties;
        }

        private List<object> _properties { get; set; }

        public object? ToObject(Type type, IEndPointContext? context)
        {
            if (type.IsInterface)
            {
                var sharedShell = typeof(SharedObjectShellShell<>).MakeGenericType(type);
                var so =
                    Activator.CreateInstance(sharedShell) as
                        ISharedObjectShell;
                if (context != null)
                    so.EndPointContext = context;

                so.Identifier = ComplexObjectIdentifier.FromFlat((ulong)_properties[0]);
                return so.UniversalValue;
            }

            var instance = Activator.CreateInstance(type);
            if (instance == null)
                return null;

            if (instance is ISharedObjectShell sharedObject && context != null)
            {
                sharedObject.EndPointContext = context;
            }

            var properties = CborSerializationToolkit.FilterProperties(type.GetProperties());
            for (var index = 0; index < properties.Count; index++)
            {
                var property = properties[index];
                property.SetValue(instance,
                    _properties[index] is IPreParsedValue ppv
                        ? ppv.ToObject(property.PropertyType, context)
                        : _properties[index]);
            }

            return instance;
        }
    }

    public class ParsedValue : IPreParsedValue
    {
        private object? _value;

        public ParsedValue(object? value)
        {
            _value = value;
        }


        public object? ToObject(Type type, IEndPointContext? context)
        {
            return _value;
        }
    }
}
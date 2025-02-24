using System;
using System.Collections;
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Linq;
using mROA.Abstract;
using mROA.Implementation;

namespace mROA.Cbor
{
    public class CborSerializaitonToolkit : IContextualSerializationToolKit
    {
        public byte[] Serialize<T>(T objectToSerialize, IEndPointContext context)
        {
            return Serialize(objectToSerialize, typeof(T), context);
        }

        public byte[] Serialize(object objectToSerialize, Type type, IEndPointContext context)
        {
        }

        public T Deserialize<T>(byte[] rawData, IEndPointContext context)
        {
            return (T)Deserialize(rawData, typeof(T), context);
        }

        public object? Deserialize(byte[] rawData, Type type, IEndPointContext context)
        {
            return Deserialize(rawData.AsSpan(), type, context);
        }

        public T Deserialize<T>(Span<byte> rawData, IEndPointContext context)
        {
            return (T)Deserialize(rawData, typeof(T), context);
        }

        public object? Deserialize(Span<byte> rawData, Type type, IEndPointContext context)
        {
            return null;
        }

        public T Cast<T>(object nonCasted, IEndPointContext context)
        {
            return (T)Cast(nonCasted, typeof(T), context);
        }

        public object Cast(object nonCasted, Type type, IEndPointContext context)
        {
            return null;
        }

        private void WriteData(object? obj, CborWriter writer)
        {
            switch (obj)
            {
                case int i:
                    writer.WriteInt32(i);
                    break;
                case long l:
                    writer.WriteInt64(l);
                    break;
                case float f:
                    writer.WriteSingle(f);
                    break;
                case double d:
                    writer.WriteDouble(d);
                    break;
                case decimal dec:
                    writer.WriteDecimal(dec);
                    break;
                case bool b:
                    writer.WriteBoolean(b);
                    break;
                case string s:
                    writer.WriteTextString(s);
                    break;
                case null:
                    writer.WriteNull();
                    break;
                case uint ui:
                    writer.WriteUInt32(ui);
                    break;
                case ulong ul:
                    writer.WriteUInt64(ul);
                    break;
                case DateTimeOffset dto:
                    writer.WriteDateTimeOffset(dto);
                    break;
                case byte[] bytes:
                    writer.WriteByteString(bytes);
                    break;
                case IDictionary dictionary:
                    WriteDictionary(dictionary, writer);
                    break;
                case IEnumerable enumerable:
                    WriteEnumerable(enumerable, writer);
                    break;
                case SharedObject sharedObject:
                    break;
                default:
                    WriteObject(obj, writer);
                    break;
            }
        }

        private void WriteEnumerable(IEnumerable enumerable, CborWriter writer)
        {
            List<object?> list = new List<object?>();

            foreach (var element in enumerable)
                list.Add(element);

            writer.WriteStartArray(list.Count);

            foreach (var element in list)
                WriteData(element, writer);

            writer.WriteEndArray();
        }

        private void WriteDictionary(IDictionary dictionary, CborWriter writer)
        {
            writer.WriteStartMap(dictionary.Count);
            var keysEnumerator = dictionary.Keys.GetEnumerator();
            var valuesEnumerator = dictionary.Values.GetEnumerator();
            for (int i = 0; i < dictionary.Count; i++)
            {
                keysEnumerator.MoveNext();
                valuesEnumerator.MoveNext();
                WriteData(keysEnumerator.Current, writer);
                WriteData(valuesEnumerator.Current, writer);
            }
            writer.WriteEndMap();
        }

        private void WriteObject(object obj, CborWriter writer)
        {
            var type = obj.GetType();
            var properties = type.GetProperties();
            var values = properties.Select(property => property.GetValue(obj));
            WriteEnumerable(values, writer);
        }
    }
}
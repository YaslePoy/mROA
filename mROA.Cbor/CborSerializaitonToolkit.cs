using System;
using System.Collections;
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using mROA.Abstract;
using mROA.Implementation;
using mROA.Implementation.Attributes;

namespace mROA.Cbor
{
    public class CborSerializaitonToolkit : IContextualSerializationToolKit
    {
        public byte[] Serialize(object objectToSerialize, IEndPointContext context)
        {
            var writer = new CborWriter();
            WriteData(objectToSerialize, writer, context);
            return writer.Encode();
        }

        public void Serialize(object objectToSerialize, Span<byte> destination, IEndPointContext context)
        {
            var writer = new CborWriter();
            WriteData(objectToSerialize, writer, context);
            writer.Encode(destination);
        }

        public T Deserialize<T>(byte[] rawData, IEndPointContext context)
        {
            return (T)Deserialize(rawData, typeof(T), context);
        }

        public object Deserialize(byte[] rawData, Type type, IEndPointContext context)
        {
            return Deserialize(rawData.AsMemory(), type, context);
        }

        public T Deserialize<T>(ReadOnlyMemory<byte> rawData, IEndPointContext context)
        {
            return (T)Deserialize(rawData, typeof(T), context);
        }

        public object Deserialize(ReadOnlyMemory<byte> rawData, Type type, IEndPointContext context)
        {
            var reader = new CborReader(rawData);
            return ReadData(reader, type, context);
        }

        public T Cast<T>(object nonCasted, IEndPointContext context)
        {
            return (T)Cast(nonCasted, typeof(T), context);
        }

        public object Cast(object nonCasted, Type type, IEndPointContext context)
        {
            return null;
        }

        private void WriteData(object? obj, CborWriter writer, IEndPointContext context)
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
                // case decimal dec:
                //     writer.WriteDecimal(dec);
                //     break;
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
                    WriteDictionary(dictionary, writer, context);
                    break;
                case IList enumerable:
                    WriteList(enumerable, writer, context);
                    break;
                case ISharedObject sharedObject:
                    sharedObject.EndPointContext = context;
                    WriteObject(sharedObject, writer, context);
                    break;
                default:
                    if (obj.GetType().IsEnum)
                    {
                        writer.WriteUInt32((uint)obj);
                        break;
                    }

                    WriteObject(obj, writer, context);
                    break;
            }
        }

        private void WriteList(IList list, CborWriter writer, IEndPointContext context)
        {



            writer.WriteStartArray(list.Count);

            foreach (var element in list)
                WriteData(element, writer, context);

            writer.WriteEndArray();
        }

        private void WriteDictionary(IDictionary dictionary, CborWriter writer, IEndPointContext context)
        {
            writer.WriteStartMap(dictionary.Count);
            var keysEnumerator = dictionary.Keys.GetEnumerator();
            var valuesEnumerator = dictionary.Values.GetEnumerator();
            for (int i = 0; i < dictionary.Count; i++)
            {
                keysEnumerator.MoveNext();
                valuesEnumerator.MoveNext();
                WriteData(keysEnumerator.Current, writer, context);
                WriteData(valuesEnumerator.Current, writer, context);
            }

            writer.WriteEndMap();
            (keysEnumerator as IDisposable)?.Dispose();
            (valuesEnumerator as IDisposable)?.Dispose();
        }

        private void WriteObject(object obj, CborWriter writer, IEndPointContext context)
        {
            var type = obj.GetType();
            var properties = FilterProperties(type.GetProperties());
            var values = properties.Select(property => property.GetValue(obj)).ToList();
            WriteList(values, writer, context);
        }

        private object ReadData(CborReader reader, Type? type, IEndPointContext context)
        {
            var state = reader.PeekState();
            switch (state)
            {
                case CborReaderState.Boolean:
                    return reader.ReadBoolean();
                case CborReaderState.UnsignedInteger:
                case CborReaderState.NegativeInteger:
                    if (type == typeof(int))
                        return reader.ReadInt32();
                    if (type == typeof(long))
                        return reader.ReadInt64();
                    if (type == typeof(uint) || type.IsEnum)
                        return reader.ReadUInt32();
                    if (type == typeof(ulong))
                        return reader.ReadUInt64();
                    break;
                case CborReaderState.ByteString:
                    return reader.ReadByteString();
                case CborReaderState.TextString:
                    return reader.ReadTextString();
                case CborReaderState.Null:
                    return null;
                case CborReaderState.DoublePrecisionFloat:
                    return reader.ReadDouble();
                case CborReaderState.SinglePrecisionFloat:
                    return reader.ReadSingle();
                case CborReaderState.StartArray:
                    if (type == null)
                        return ReadList(reader, null, context);

                    if (type.IsSubclassOf(typeof(ISharedObject)))
                        return ReadSharedObject(reader, type, context);
                    
                    if (type.IsSubclassOf(typeof(IList)) || type.IsArray)
                        return ReadList(reader, type, context);
                    
                    return ReadObject(reader, type, context);

                case CborReaderState.StartMap:
                    return ReadDictionary(reader, type, context);
            }


            if (type == typeof(DateTimeOffset))
                return reader.ReadDateTimeOffset();

            return null;
        }


        private Array ReadList(CborReader reader, Type? type, IEndPointContext context)
        {
            var length = reader.ReadStartArray();
            if (length != null)
            {
                var values = new object[length.Value];

                Type elementType = typeof(object);

                if (type is { IsArray: true })
                    elementType = type.GetGenericArguments()[0];


                for (int i = 0; i < length; i++)
                {
                    values[i] = ReadData(reader, elementType, context);
                }
            }

            return Array.Empty<object>();
        }

        private IDictionary ReadDictionary(CborReader reader, Type type, IEndPointContext context)
        {
            var dictionaryInstance = (Activator.CreateInstance(type) as IDictionary)!;
            var length = reader.ReadStartArray();
            if (length != null)
            {
                for (int i = 0; i < length; i++)
                {
                    var key = ReadData(reader, type, context);
                    var value = ReadData(reader, type, context);
                    dictionaryInstance.Add(key, value);
                }
            }

            return dictionaryInstance;
        }

        private object ReadObject(CborReader reader, Type type, IEndPointContext context)
        {
            var instance = Activator.CreateInstance(type)!;

            FillObject(instance, type, reader, context);

            return instance;
        }

        private ISharedObject ReadSharedObject(CborReader reader, Type type, IEndPointContext context)
        {
            var sharedObject = (Activator.CreateInstance(type) as ISharedObject)!;
            sharedObject.EndPointContext = context;

            FillObject(sharedObject, type, reader, context);

            return sharedObject;
        }

        private void FillObject(object obj, Type type, CborReader reader, IEndPointContext context)
        {
            var properties = FilterProperties(type.GetProperties());

            _ = reader.ReadStartArray();

            foreach (var property in properties)
            {
                var value = ReadData(reader, property.PropertyType, context);
                property.SetValue(obj, value);
            }

            reader.ReadEndArray();
        }

        private List<PropertyInfo> FilterProperties(PropertyInfo[] properties)
        {
            var finalProperties = new List<PropertyInfo>(properties.Length);
            foreach (var property in properties)
            {
                if (property.GetCustomAttribute<SerializationIgnoreAttribute>() == null)
                    finalProperties.Add(property);
            }

            return finalProperties;
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Linq;
using System.Reflection;
using mROA.Abstract;
using mROA.Implementation;
using mROA.Implementation.Attributes;

namespace mROA.Cbor
{
    public class CborSerializationToolkit : IContextualSerializationToolKit
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

        public T Deserialize<T>(byte[] rawData, IEndPointContext? context)
        {
            return (T)Deserialize(rawData, typeof(T), context) ?? default;
        }

        public object? Deserialize(byte[] rawData, Type type, IEndPointContext? context)
        {
            return Deserialize(rawData.AsMemory(), type, context);
        }

        public T Deserialize<T>(ReadOnlyMemory<byte> rawMemory, IEndPointContext? context)
        {
            return (T)Deserialize(rawMemory, typeof(T), context);
        }

        public object? Deserialize(ReadOnlyMemory<byte> rawMemory, Type type, IEndPointContext? context)
        {
            var reader = new CborReader(rawMemory);
            return ReadData(reader, type, context);
        }

        public T Cast<T>(object nonCasted, IEndPointContext? context)
        {
            return (T)Cast(nonCasted, typeof(T), context);
        }

        public object? Cast(object? nonCasted, Type type, IEndPointContext? context)
        {
            if (nonCasted == null)
                return null;

            if (nonCasted.GetType() == type)
                return nonCasted;

            if (nonCasted is PreParsedValue preParsed)
                return preParsed.ToObject(type, context);
            return Convert.ChangeType(nonCasted, type);
        }

        private void WriteData(object? obj, CborWriter writer, IEndPointContext? context)
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
                case Guid g:
                    writer.WriteByteString(g.ToByteArray());
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
                case ISharedObjectShell sharedObject:
                    if (context != null)
                        sharedObject.EndPointContext = context;
                    WriteObject(sharedObject, writer, context);
                    break;
                default:
                    if (obj.GetType().IsEnum)
                    {
                        writer.WriteInt32((int)obj);
                        break;
                    }

                    WriteObject(obj, writer, context);
                    break;
            }
        }

        private void WriteList(IList list, CborWriter writer, IEndPointContext? context)
        {
            writer.WriteStartArray(list.Count);

            foreach (var element in list)
                WriteData(element, writer, context);

            writer.WriteEndArray();
        }

        private void WriteDictionary(IDictionary dictionary, CborWriter writer, IEndPointContext? context)
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

        private void WriteObject(object obj, CborWriter writer, IEndPointContext? context)
        {
            var type = obj.GetType();

            if (obj is IShared)
            {
                var generic = obj.GetType().GetInterfaces().FirstOrDefault(i => typeof(IShared).IsAssignableFrom(i));
                var sharedShell = typeof(SharedObjectShellShell<>).MakeGenericType(generic);
                var so =
                    Activator.CreateInstance(sharedShell, obj) as
                        ISharedObjectShell;
                if (context != null)
                    so.EndPointContext = context;

                writer.WriteStartArray(1);
                writer.WriteUInt64(so.Identifier.Flat);
                writer.WriteEndArray();

                return;
            }

            var properties = FilterProperties(type.GetProperties());
            var values = properties.Select(property => property.GetValue(obj)).ToList();
            WriteList(values, writer, context);
        }

        private object? ReadData(CborReader reader, Type? type, IEndPointContext? context)
        {
            var state = reader.PeekState();
            switch (state)
            {
                case CborReaderState.Boolean:
                    return reader.ReadBoolean();
                case CborReaderState.UnsignedInteger:
                case CborReaderState.NegativeInteger:
                    if (type == typeof(int) || type is { IsEnum: true })
                        return reader.ReadInt32();
                    if (type == typeof(long))
                        return reader.ReadInt64();
                    if (type == typeof(uint))
                        return reader.ReadUInt32();
                    if (type == typeof(ulong))
                        return reader.ReadUInt64();

                    return reader.ReadUInt64();
                case CborReaderState.ByteString:
                    if (type == typeof(Guid))
                        return new Guid(reader.ReadByteString());
                    return reader.ReadByteString();
                case CborReaderState.TextString:
                    return reader.ReadTextString();
                case CborReaderState.Null:
                    reader.ReadNull();
                    return null;
                case CborReaderState.DoublePrecisionFloat:
                    return reader.ReadDouble();
                case CborReaderState.SinglePrecisionFloat:
                    return reader.ReadSingle();
                case CborReaderState.HalfPrecisionFloat:
                    return type == typeof(float) ? reader.ReadSingle() : reader.ReadDouble();

                case CborReaderState.StartArray:
                    if (type == null)
                        return ReadList(reader, null, context);

                    if (type.IsSubclassOf(typeof(ISharedObjectShell)))
                        return ReadSharedObject(reader, type, context);

                    if (typeof(IList).IsAssignableFrom(type) || type.IsArray)
                        return ReadList(reader, type, context);

                    return ReadObject(reader, type, context);

                case CborReaderState.StartMap:
                    return ReadDictionary(reader, type, context);
            }


            if (type == typeof(DateTimeOffset))
                return reader.ReadDateTimeOffset();

            return null;
        }


        private IList ReadList(CborReader reader, Type? type, IEndPointContext? context)
        {
            var length = reader.ReadStartArray();
            if (length != null)
            {
                var elementType = typeof(object);
                if (type is { IsArray: true })
                {
                    elementType = type.GetElementType();
                    Array values = Array.CreateInstance(elementType, length.Value);


                    for (int i = 0; i < length; i++)
                    {
                        values.SetValue(ReadData(reader, elementType, context), i);
                    }

                    reader.ReadEndArray();
                    return values;
                }

                if (typeof(IList).IsAssignableFrom(type))
                    elementType = type.GetGenericArguments()[0];
                else elementType = typeof(object);
                Type genericListType = typeof(List<>).MakeGenericType(elementType);
                var list = (IList)Activator.CreateInstance(genericListType, length);

                for (int i = 0; i < length; i++)
                {
                    list.Add(ReadData(reader, elementType, context));
                }

                reader.ReadEndArray();


                return list;


                return null;
            }

            return null;
        }

        private IDictionary ReadDictionary(CborReader reader, Type type, IEndPointContext? context)
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

        private object ReadObject(CborReader reader, Type type, IEndPointContext? context)
        {
            if (type == typeof(object))
            {
                return new PreParsedValue(ReadList(reader, null, context) as List<object>);
            }

            if (type.IsInterface)
            {
                var sharedShell = typeof(SharedObjectShellShell<>).MakeGenericType(type);
                var so =
                    Activator.CreateInstance(sharedShell) as
                        ISharedObjectShell;
                if (context != null)
                    so.EndPointContext = context;

                reader.ReadStartArray();
                var identifier = reader.ReadUInt64();
                reader.ReadEndArray();

                so.Identifier = UniversalObjectIdentifier.FromFlat(identifier);
                return so.UniversalValue;
            }

            var instance = Activator.CreateInstance(type)!;

            FillObject(instance, type, reader, context);

            return instance;
        }

        private ISharedObjectShell ReadSharedObject(CborReader reader, Type type, IEndPointContext? context)
        {
            var sharedObject = (Activator.CreateInstance(type) as ISharedObjectShell)!;
            if (context != null)
            {
                sharedObject.EndPointContext = context;
            }

            FillObject(sharedObject, type, reader, context);

            return sharedObject;
        }

        private void FillObject(object obj, Type type, CborReader reader, IEndPointContext? context)
        {
            var propertyInfos = type.GetProperties();
            var properties = FilterProperties(propertyInfos);

            var length = reader.ReadStartArray();
            try
            {
#if TRACE
                Console.WriteLine($"Reading list of {length} objects, {properties.Count} properties found");
#endif

                for (var index = 0; index < length; index++)
                {
                    var property = properties[index];
                    var value = ReadData(reader, property.PropertyType, context);
                    property.SetValue(obj, value);
                }

                reader.ReadEndArray();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                reader.ReadEndArray();

                throw;
            }
        }

        public static List<PropertyInfo> FilterProperties(PropertyInfo[] properties)
        {
            var finalProperties = new List<PropertyInfo>(properties.Length);
            foreach (var property in properties.Where(i => i.CanWrite && i.CanRead))
            {
                if (property.GetCustomAttribute<SerializationIgnoreAttribute>() == null)
                    finalProperties.Add(property);
            }

            return finalProperties;
        }

        public void Inject<T>(T dependency)
        {
        }

        public byte[] Serialize<T>(T objectToSerialize)
        {
            return Serialize(objectToSerialize, typeof(T));
        }

        public byte[] Serialize(object objectToSerialize, Type type)
        {
            return Serialize(objectToSerialize, context: null);
        }

        public T Deserialize<T>(byte[] rawData)
        {
            return Deserialize<T>(rawData: rawData, context: null);
        }

        public object? Deserialize(byte[] rawData, Type type)
        {
            return Deserialize(rawData: rawData, type, context: null);
        }

        public T Deserialize<T>(Span<byte> rawData)
        {
            return Deserialize<T>(rawData.ToArray().AsMemory(), context: null);
        }

        public object? Deserialize(Span<byte> rawData, Type type)
        {
            return Deserialize(rawData: rawData.ToArray(), type: type);
        }

        public T Cast<T>(object nonCasted)
        {
            return Cast<T>(nonCasted: nonCasted, context: null);
        }

        public object Cast(object nonCasted, Type type)
        {
            return Cast(nonCasted: nonCasted, type: type, context: null);
        }
    }
}
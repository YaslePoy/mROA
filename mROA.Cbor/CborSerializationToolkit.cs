using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Cbor;
using System.Linq;
using System.Reflection;
using mROA.Abstract;
using mROA.Implementation;
using mROA.Implementation.Attributes;
using mROA.Implementation.CommandExecution;

namespace mROA.Cbor
{
    public class CborSerializationToolkit : IContextualSerializationToolKit
    {
        private readonly IOrdinaryStructureParser[] _parsers = {
            new NetworkMessageHeaderParser(), new DefaultCallRequestParser(), new FinalCommandExecutionParser(),
            new FinalCommandExecutionResultlessParser()
        };
        // private Dictionary<Type, IOrdinaryStructureParser> _parsers = new(){{typeof(NetworkMessageHeader), new NetworkMessageHeaderParser()}, {typeof(DefaultCallRequest), new DefaultCallRequestParser()}, {typeof(FinalCommandExecution<object>), new FinalCommandExecutionParser()}, {typeof(FinalCommandExecution), new FinalCommandExecutionResultlessParser()}}; //
        private Dictionary<Type, List<PropertyInfo>> _propertiesCache = new();
        public static TimeSpan SerializationTime = TimeSpan.Zero;

        private bool FindParser(Type t, out IOrdinaryStructureParser parser)
        {
            if (t == typeof(NetworkMessageHeader))
            {
                parser = _parsers[0];
                return true;
            }

            if (t == typeof(DefaultCallRequest))
            {
                parser = _parsers[1];
                return true;
            }

            if (t == typeof(FinalCommandExecution<object>))
            {
                parser = _parsers[2];
                return true;
            }

            if (t == typeof(FinalCommandExecution))
            {
                parser = _parsers[3];
                return true;
            }

            parser = null;
            return false;
        }
        public byte[] Serialize(object objectToSerialize, IEndPointContext context)
        {
            var sw = Stopwatch.StartNew();
            var writer = new CborWriter(initialCapacity:64);
            WriteData(objectToSerialize, writer, context);
            var result = writer.Encode();
            sw.Stop();
            SerializationTime = SerializationTime.Add(sw.Elapsed);
            return result;
        }

        public int Serialize(object objectToSerialize, Span<byte> destination, IEndPointContext context)
        {
            var writer = new CborWriter(initialCapacity:64);
            WriteData(objectToSerialize, writer, context);
            return writer.Encode(destination);
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


            if (type == typeof(Guid))
            {
                return new Guid((byte[])nonCasted);
            }

            return Convert.ChangeType(nonCasted, type);
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

        public void WriteData(object? obj, CborWriter writer, IEndPointContext? context)
        {
            if (obj is not null && FindParser(obj.GetType(), out var parser))
            {
                parser.Write(writer, obj, context, this);
                return;
            }
            
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

        public void WriteList(IList list, CborWriter writer, IEndPointContext? context)
        {
            writer.WriteStartArray(list.Count);

            for (var index = 0; index < list.Count; index++)
            {
                var element = list[index];
                WriteData(element, writer, context);
            }

            writer.WriteEndArray();
        }

        public void WriteDictionary(IDictionary dictionary, CborWriter writer, IEndPointContext? context)
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

        public void WriteObject(object obj, CborWriter writer, IEndPointContext? context)
        {
            var type = obj.GetType();

            if (obj is IShared)
            {
                var sharedShell = typeof(SharedObjectShellShell<object>);
                var so =
                    Activator.CreateInstance(sharedShell, obj, context) as
                        ISharedObjectShell;

                writer.WriteStartArray(1);
                writer.WriteUInt64(so.Identifier.Flat);
                writer.WriteEndArray();

                return;
            }

            var properties = GetAvailableProperties(type);
            var values = properties.Select(property => property.GetValue(obj)).ToList();
            WriteList(values, writer, context);
        }

        public object? ReadData(CborReader reader, Type? type, IEndPointContext? context)
        {
            if (type is not null && FindParser(type, out var parser))
            {
                return parser.Read(reader, context, this);
            }
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


        public IList ReadList(CborReader reader, Type? type, IEndPointContext? context)
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

        public IDictionary ReadDictionary(CborReader reader, Type type, IEndPointContext? context)
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

        public object ReadObject(CborReader reader, Type type, IEndPointContext? context)
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

                so.Identifier = ComplexObjectIdentifier.FromFlat(identifier);
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

        public void FillObject(object obj, Type type, CborReader reader, IEndPointContext? context)
        {
            var properties = GetAvailableProperties(type);
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

                if (reader.PeekState() == CborReaderState.EndArray)
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
            
            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                if (property is not { CanRead: true, CanWrite: true } || property.GetCustomAttribute<SerializationIgnoreAttribute>() != null)
                    continue;
                
                finalProperties.Add(property);
                
            }

            return finalProperties;
        }

        private List<PropertyInfo> GetAvailableProperties(Type type)
        {
            if (_propertiesCache.TryGetValue(type, out var properties))
            {
                return properties;
            }

            var propertiesList = FilterProperties(type.GetProperties());
            _propertiesCache[type] = propertiesList;
            return propertiesList;
        }
    }
}
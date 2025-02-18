using System.Text.Json;
using mROA.Abstract;

namespace mROA.Implementation;

public class JsonSerializationToolkit : ISerializationToolkit
{
    public byte[] Serialize<T>(T objectToSerialize)
    {
        return JsonSerializer.SerializeToUtf8Bytes(objectToSerialize);
    }

    public byte[] Serialize(object objectToSerialize, Type type)
    {
        return JsonSerializer.SerializeToUtf8Bytes(objectToSerialize, type);
    }

    public T? Deserialize<T>(byte[] rawData)
    {
        return JsonSerializer.Deserialize<T>(rawData);
    }

    public object? Deserialize(byte[] rawData, Type type)
    {
        return JsonSerializer.Deserialize(rawData, type);
    }

    public T? Deserialize<T>(Span<byte> rawData)
    {
        return JsonSerializer.Deserialize<T>(rawData);
    }

    public object? Deserialize(Span<byte> rawData, Type type)
    {
        return JsonSerializer.Deserialize(rawData, type);
    }

    public T Cast<T>(object nonCasted)
    {
        return nonCasted switch
        {
            JsonElement jsonElement => jsonElement.Deserialize<T>()!,
            T casted => casted,
            _ => throw new JsonException("Cannot cast object to type " + typeof(T).FullName)
        };
    }

    public object Cast(object nonCasted, Type type)
    {
        if (nonCasted is JsonElement jsonElement)
            return jsonElement.Deserialize(type)!;

        throw new JsonException("Cannot cast object to type " + type.FullName);
    }

    public void Inject<T>(T dependency)
    {
    }
}
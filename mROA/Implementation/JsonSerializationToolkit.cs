using System.Text.Json;

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
}
namespace mROA.Implementation;

public interface ISerializationToolkit
{
    public byte[] Serialize<T>(T objectToSerialize);
    public byte[] Serialize(object objectToSerialize, Type type);
    public T? Deserialize<T>(byte[] rawData);
    public object? Deserialize(byte[] rawData, Type type);
}
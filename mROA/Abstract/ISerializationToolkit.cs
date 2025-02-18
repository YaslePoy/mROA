namespace mROA.Abstract;

public interface ISerializationToolkit : IInjectableModule
{
    byte[] Serialize<T>(T objectToSerialize);
    byte[] Serialize(object objectToSerialize, Type type);
    T? Deserialize<T>(byte[] rawData);
    object? Deserialize(byte[] rawData, Type type);
    T Cast<T>(object nonCasted);
    object Cast(object nonCasted, Type type);

}
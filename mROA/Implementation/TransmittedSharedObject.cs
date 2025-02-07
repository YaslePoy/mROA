using System.Text.Json.Serialization;
using mROA.Abstract;

namespace mROA.Implementation;

public static class TransmissionConfig
{
    public static IContextRepository? DefaultContextRepository { get; set; }
}

public class TransmittedSharedObject<T> where T : notnull
{
    private static IContextRepository GetDefaultContextRepository() => TransmissionConfig.DefaultContextRepository ??
                                                                       throw new NullReferenceException(
                                                                           "DefaultContextRepository was not defined");

    private int ContextId { get; init; } = -1;

    [JsonIgnore]
    public T? Value => GetDefaultContextRepository().GetObject<T>(ContextId);

    private TransmittedSharedObject()
    {
    }
    public TransmittedSharedObject(T value) 
    {
        ContextId = GetDefaultContextRepository().GetObjectIndex(value);
    }

    public static implicit operator T(TransmittedSharedObject<T> value) => value.Value!;

    public static implicit operator TransmittedSharedObject<T>(T value) =>
        new() { ContextId = GetDefaultContextRepository().GetObjectIndex(value) };
}
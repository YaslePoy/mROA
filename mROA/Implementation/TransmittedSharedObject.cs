using System.Text.Json.Serialization;

namespace mROA.Implementation;

public static class TransmissionConfig
{
    public static IContextRepository DefaultContextRepository { get; set; }
}

public class TransmittedSharedObject<T>
{
    public int ContextId { get; set; } = -1;

    [JsonIgnore]
    public T? Value => TransmissionConfig.DefaultContextRepository.GetObject<T>(ContextId);

    [JsonIgnore]
    public IContextRepository Context { get; set; }

    public static implicit operator T(TransmittedSharedObject<T> value) => value.Value!;

    public static implicit operator TransmittedSharedObject<T>(T value) =>
        new() { ContextId = TransmissionConfig.DefaultContextRepository.GetObjectIndex(value) };
}
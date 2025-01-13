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
    public T Value { get; set; }

    [JsonIgnore]
    public IContextRepository Context { get; set; }

    public static implicit operator T(TransmittedSharedObject<T> value) => value.Value;

    public static implicit operator TransmittedSharedObject<T>(T value) =>
        new() { Value = value, ContextId = TransmissionConfig.DefaultContextRepository.GetObjectIndex(value) };

    // public void Fill() => Value = (T)TransmissionConfig.DefaultContextRepository.GetObject(ContextId);
    // public void Collect() => ContextId = TransmissionConfig.DefaultContextRepository.GetObjectIndex(ContextId);
}
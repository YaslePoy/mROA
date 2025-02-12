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

    private int _contextId = -1;

    // ReSharper disable once MemberCanBePrivate.Global
    public int ContextId
    {
        get => _contextId;
        init
        {
            _contextId = value;
            Value = GetDefaultContextRepository().GetObject<T>(_contextId)!;
        }
    }

    [JsonIgnore]
    public T Value { get; private set; }

    public TransmittedSharedObject()
    {
    }
    // ReSharper disable once UnusedMember.Global
    public TransmittedSharedObject(T value) 
    {
        Value = value;
        _contextId = GetDefaultContextRepository().GetObjectIndex(value);
    }

    public static implicit operator T(TransmittedSharedObject<T> value) => value.Value;

    public static implicit operator TransmittedSharedObject<T>(T value) =>
        new() { ContextId = GetDefaultContextRepository().GetObjectIndex(value) };
}
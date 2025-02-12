using System.Text.Json.Serialization;
using mROA.Abstract;

namespace mROA.Implementation;

public static class TransmissionConfig
{
    public static IContextRepository? RealContextRepository { get; set; }
    public static IContextRepository? RemoteEndpointContextRepository { get; set; }
    public static int ProcessOwnerId { get; set; }
}

public class TransmittedSharedObject<T> where T : notnull
{
    private IContextRepository GetDefaultContextRepository() => (OwnerId == TransmissionConfig.ProcessOwnerId ? TransmissionConfig.RealContextRepository : TransmissionConfig.RemoteEndpointContextRepository) ??
                                                                       throw new NullReferenceException(
                                                                           "DefaultContextRepository was not defined");

    private int _contextId = -1;
    public int OwnerId { get; init; }
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
        new() { ContextId = value is IRemoteObject ro ? TransmissionConfig.RemoteEndpointContextRepository!.GetObjectIndex(ro) : TransmissionConfig.RealContextRepository!.GetObjectIndex(value) };
}
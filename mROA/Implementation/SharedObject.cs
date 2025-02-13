using System.Text.Json.Serialization;
using mROA.Abstract;

namespace mROA.Implementation;

public static class TransmissionConfig
{
    public static IContextRepository? RealContextRepository { get; set; }
    public static IContextRepository? RemoteEndpointContextRepository { get; set; }
    public static int ProcessOwnerId { get; set; }
    public static Dictionary<int, int> ThreadsOwners { get; } = new();
}

public class SharedObject<T> where T : notnull
{
    private IContextRepository GetDefaultContextRepository() =>
        (OwnerId == TransmissionConfig.ProcessOwnerId
            ? TransmissionConfig.RealContextRepository
            : TransmissionConfig.RemoteEndpointContextRepository) ??
        throw new NullReferenceException(
            "DefaultContextRepository was not defined");

    private int _contextId = -1;
    public int OwnerId { get; }

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

    [JsonIgnore] public T Value { get; private set; }

    public SharedObject()
    {
    }

    // ReSharper disable once UnusedMember.Global
    public SharedObject(T value)
    {
        Value = value;
        _contextId = GetDefaultContextRepository().GetObjectIndex(value);
        OwnerId = TransmissionConfig.ThreadsOwners.TryGetValue(Environment.CurrentManagedThreadId, out var ownerId)
            ? ownerId
            : TransmissionConfig.ProcessOwnerId;
    }

    public static implicit operator T(SharedObject<T> value) => value.Value;

    public static implicit operator SharedObject<T>(T value) =>
        new()
        {
            ContextId = value is IRemoteObject ro
                ? TransmissionConfig.RemoteEndpointContextRepository!.GetObjectIndex(ro)
                : TransmissionConfig.RealContextRepository!.GetObjectIndex(value)
        };
}
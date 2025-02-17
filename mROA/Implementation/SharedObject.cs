using System.Text.Json.Serialization;
using mROA.Abstract;

namespace mROA.Implementation;

public static class TransmissionConfig
{
    public static IContextRepository? RealContextRepository { get; set; }
    public static IContextRepository? RemoteEndpointContextRepository { get; set; }
    public static IOwnershipRepository? OwnershipRepository { get; set; }
}

public class SharedObject<T> where T : notnull
{
    private IContextRepository GetDefaultContextRepository() =>
        (OwnerId == TransmissionConfig.OwnershipRepository!.GetHostOwnershipId()
            ? TransmissionConfig.RealContextRepository
            : TransmissionConfig.RemoteEndpointContextRepository) ??
        throw new NullReferenceException(
            "DefaultContextRepository was not defined");

    private int _contextId = -2;
    private int _ownerId = -1;

    public int OwnerId
    {
        get
        {
            _ownerId = _ownerId == -1 ? TransmissionConfig.OwnershipRepository!.GetOwnershipId() : _ownerId;
            return _ownerId;
        }
        set => _ownerId = value;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public int ContextId
    {
        // ReSharper disable once UnusedMember.Global
        get
        {
            if (_contextId != -2)
                return _contextId;
            
            _contextId = TransmissionConfig.RealContextRepository!.GetObjectIndex(Value);
            return _contextId;
        }
        init
        {
            _contextId = value;
            Value = GetDefaultContextRepository().GetObject<T>(_contextId)!;
        }
    }

    [JsonIgnore] public T Value { get; private set; }

    // ReSharper disable once MemberCanBePrivate.Global
    public SharedObject()
    {
    }

    // ReSharper disable once UnusedMember.Global
    public SharedObject(T value)
    {
        Value = value;

        if (value is RemoteObjectBase ro)
        {
            _ownerId = ro.OwnerId;
            _contextId = ro.Id;
        }
        
        _ownerId = TransmissionConfig.OwnershipRepository!.GetHostOwnershipId();
    }

    public static implicit operator T(SharedObject<T> value) => value.Value;

    public static implicit operator SharedObject<T>(T value) =>
        new(value);
}
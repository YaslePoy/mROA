using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using mROA.Abstract;
using mROA.Implementation.Attributes;

// ReSharper disable UnusedMember.Global
#pragma warning disable CS8618, CS9264

namespace mROA.Implementation
{
    public static class TransmissionConfig
    {
#if TRACE
        public static int TotalTransmittedBytes { get; set; }
#endif
        private static IContextRepository? _realContextRepository;
        private static IContextRepository? _remoteEndpointContextRepository;
        private static IOwnershipRepository? _ownershipRepository;

        public static IContextRepository RealContextRepository
        {
            get => _realContextRepository ?? throw new NullReferenceException("RealContextRepository is null");
            set => _realContextRepository = value;
        }

        public static IContextRepository RemoteEndpointContextRepository
        {
            get => _remoteEndpointContextRepository ??
                   throw new NullReferenceException("RemoteEndpointContextRepository is null");
            set => _remoteEndpointContextRepository = value;
        }

        public static IOwnershipRepository OwnershipRepository
        {
            get => _ownershipRepository ?? throw new NullReferenceException("OwnershipRepository is null");
            set => _ownershipRepository = value;
        }
    }

    public interface ISharedObject
    {
        IEndPointContext EndPointContext { get; set; }
    }

    public class SharedObject<T> : ISharedObject where T : notnull
    {
        [SerializationIgnore]
        [JsonIgnore]
        public IEndPointContext EndPointContext { get; set; } = new EndPointContext
        {
            RealRepository = TransmissionConfig.RealContextRepository,
            RemoteRepository = TransmissionConfig.RemoteEndpointContextRepository,
            HostId = TransmissionConfig.OwnershipRepository.GetHostOwnershipId(),
            OwnerFunc = TransmissionConfig.OwnershipRepository.GetOwnershipId
        };

        private IContextRepository GetDefaultContextRepository() =>
            (_identifier.OwnerId == EndPointContext.HostId
                ? EndPointContext.RealRepository
                : EndPointContext.RemoteRepository) ??
            throw new NullReferenceException(
                "DefaultContextRepository was not defined");

        private UniversalObjectIdentifier _identifier = UniversalObjectIdentifier.Null;

        public UniversalObjectIdentifier Identifier
        {
            get
            {
                _identifier.OwnerId = _identifier.OwnerId == -1 ? EndPointContext.OwnerId : _identifier.OwnerId;
                return _identifier;
            }
            set
            {
                _identifier = value;
                Value = GetDefaultContextRepository().GetObjectBySharedObject(this);
            }
        }


        // public int OwnerId
        // {
        //     get
        //     {
        //         _ownerId = _ownerId == -1 ? EndPointContext.OwnerId : _ownerId;
        //         return _ownerId;
        //     }
        //     set => _ownerId = value;
        // }
        //
        // // ReSharper disable once MemberCanBePrivate.Global
        // public int ContextId
        // {
        //     // ReSharper disable once UnusedMember.Global
        //     get
        //     {
        //         if (_contextId != -2)
        //             return _contextId;
        //
        //         _contextId = EndPointContext.RealRepository.GetObjectIndex(Value);
        //         return _contextId;
        //     }
        //     set
        //     {
        //         _contextId = value;
        //         Value = GetDefaultContextRepository().GetObjectBySharedObject(this);
        //     }
        // }

        [JsonIgnore] [SerializationIgnore] public T Value { get; private set; }

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedMember.Global
        public SharedObject()
        {
        }

        // ReSharper disable once UnusedMember.Global
        public SharedObject(T value)
        {
            Value = value;

            if (value is RemoteObjectBase ro)
            {
                _identifier = ro.Identifier;
            }
            else
            {
                _identifier.OwnerId = EndPointContext.HostId;
                _identifier.ContextId = EndPointContext.RealRepository.GetObjectIndex(Value);
            }
        }

        public static implicit operator T(SharedObject<T> value) => value.Value;

        public static implicit operator SharedObject<T>(T value) =>
            new(value);
    }
}
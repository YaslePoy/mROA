using System;
using System.Text.Json.Serialization;
using mROA.Abstract;
// ReSharper disable UnusedMember.Global
#pragma warning disable CS8618, CS9264

namespace mROA.Implementation
{
    public static class TransmissionConfig
    {
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
            get => _remoteEndpointContextRepository ?? throw new NullReferenceException("RemoteEndpointContextRepository is null");
            set => _remoteEndpointContextRepository = value;
        }

        public static IOwnershipRepository OwnershipRepository
        {
            get => _ownershipRepository ?? throw new NullReferenceException("OwnershipRepository is null");
            set => _ownershipRepository = value;
        }

    }

    public class SharedObject : SharedObject<object>
    {
        
    }
    public class SharedObject<T> where T : notnull
    {
        private IContextRepository GetDefaultContextRepository() =>
            (OwnerId == TransmissionConfig.OwnershipRepository.GetHostOwnershipId()
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
                _ownerId = _ownerId == -1 ? TransmissionConfig.OwnershipRepository.GetOwnershipId() : _ownerId;
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

                _contextId = TransmissionConfig.RealContextRepository.GetObjectIndex(Value);
                return _contextId;
            }
            set
            {
                _contextId = value;
                Value = GetDefaultContextRepository().GetObjectBySharedObject(this);
            }
        }

        [JsonIgnore] public T Value { get; private set; }

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
                _ownerId = ro.OwnerId;
                _contextId = ro.Id;
            }
            else
                _ownerId = TransmissionConfig.OwnershipRepository.GetHostOwnershipId();
        }

        public static implicit operator T(SharedObject<T> value) => value.Value;

        public static implicit operator SharedObject<T>(T value) =>
            new(value);
    }
}
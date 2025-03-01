﻿using System;
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
            (OwnerId == EndPointContext.HostId
                ? EndPointContext.RealRepository
                : EndPointContext.RemoteRepository) ??
            throw new NullReferenceException(
                "DefaultContextRepository was not defined");

        private int _contextId = -2;
        private int _ownerId = -1;

        public int OwnerId
        {
            get
            {
                _ownerId = _ownerId == -1 ? EndPointContext.OwnerId : _ownerId;
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

                _contextId = EndPointContext.RealRepository.GetObjectIndex(Value);
                return _contextId;
            }
            set
            {
                _contextId = value;
                Value = GetDefaultContextRepository().GetObjectBySharedObject(this);
            }
        }

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
                _ownerId = ro.OwnerId;
                _contextId = ro.Id;
            }
            else
                _ownerId = EndPointContext.HostId;
        }

        public static implicit operator T(SharedObject<T> value) => value.Value;

        public static implicit operator SharedObject<T>(T value) =>
            new(value);
    }

    public struct UniversalObjectIdentifier : IEquatable<UniversalObjectIdentifier>
    {
        public int ContextId;
        public int OwnerId;

        public override string ToString()
        {
            return $"{nameof(ContextId)}: {ContextId}, {nameof(OwnerId)}: {OwnerId}";
        }

        public bool IsStatic => ContextId == -1;
        
        public ulong Flat
        {
            get
            {
                return (ulong)OwnerId << 32 | (uint)ContextId;
            }
            set
            {
                OwnerId = (int)(value >> 32);
                ContextId = (int)(value & 0xFFFFFFFF);
            }
        }

        public bool Equals(UniversalObjectIdentifier other)
        {
            return ContextId == other.ContextId && OwnerId == other.OwnerId;
        }

        public override bool Equals(object? obj)
        {
            return obj is UniversalObjectIdentifier other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ContextId, OwnerId);
        }
    }
}
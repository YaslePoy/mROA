using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using mROA.Abstract;
using mROA.Implementation.Attributes;

// ReSharper disable UnusedMember.Global
#pragma warning disable CS8618, CS9264

namespace mROA.Implementation
{
    public interface ISharedObjectShell
    {
        IEndPointContext EndPointContext { get; set; }
        UniversalObjectIdentifier Identifier { get; set; }
        object UniversalValue { get; set; }
    }

    public class SharedObjectShellShell<T> : ISharedObjectShell where T : notnull
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

        public object UniversalValue
        {
            get => _value;
            set => _value = (T)value;
        }

        private T _value;

        [JsonIgnore]
        [SerializationIgnore]
        public T Value
        {
            get => _value;
            set
            {
                _value = value;

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
        }

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedMember.Global
        public SharedObjectShellShell()
        {
        }

        // ReSharper disable once UnusedMember.Global
        public SharedObjectShellShell(T value)
        {
            Value = value;
        }

        public static implicit operator T(SharedObjectShellShell<T> value) => value.Value;

        public static implicit operator SharedObjectShellShell<T>(T value) =>
            new(value);
    }
}
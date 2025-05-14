using System;
using System.Text.Json.Serialization;
using mROA.Abstract;
using mROA.Implementation.Attributes;

// ReSharper disable UnusedMember.Global
// #pragma warning disable CS8618, CS9264

namespace mROA.Implementation
{
    public interface ISharedObjectShell
    {
        // ReSharper disable once UnusedMemberInSuper.Global
        IEndPointContext EndPointContext { get; set; }
        ComplexObjectIdentifier Identifier { get; set; }
        object UniversalValue { get; set; }
    }

    public class SharedObjectShellShell<T> : ISharedObjectShell where T : notnull
    {
        private ComplexObjectIdentifier _identifier = ComplexObjectIdentifier.Null;

        private T? _value;

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedMember.Global
        public SharedObjectShellShell()
        {
            _value = default;
            EndPointContext = new EndPointContext();
        }

        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public SharedObjectShellShell(T value, IEndPointContext endPointContext)
        {
            EndPointContext = endPointContext;
            Value = value;
        }

        [JsonIgnore]
        [SerializationIgnore]
        // ReSharper disable once MemberCanBePrivate.Global
        public T Value
        {
            get => _value!;
            set
            {
                _value = value;

                if (value is RemoteObjectBase ro)
                {
                    _identifier = ro.Identifier;
                }
                else
                {
                    _identifier.OwnerId = EndPointContext.OwnerId;
                    _identifier.ContextId = EndPointContext.RealRepository.GetObjectIndex<T>(Value, EndPointContext);
                }
            }
        }

        [SerializationIgnore] [JsonIgnore] public IEndPointContext EndPointContext { get; set; }

        public ComplexObjectIdentifier Identifier
        {
            get
            {
                _identifier.OwnerId = _identifier.OwnerId == 0 ? EndPointContext.OwnerId : _identifier.OwnerId;
                return _identifier;
            }
            set
            {
                _identifier = value;
                Value = GetDefaultContextRepository().GetObject<T>(Identifier, EndPointContext);
            }
        }

        public object UniversalValue
        {
            get => _value!;
            set => _value = (T)value;
        }

        private IInstanceRepository GetDefaultContextRepository() =>
            (_identifier.OwnerId == EndPointContext.OwnerId
                ? EndPointContext.RealRepository
                : EndPointContext.RemoteRepository) ??
            throw new NullReferenceException(
                "DefaultContextRepository was not defined");

        public static implicit operator T(SharedObjectShellShell<T> value) => value.Value;

        // public static implicit operator SharedObjectShellShell<T>(T value) =>
        //     new(value);
    }
}
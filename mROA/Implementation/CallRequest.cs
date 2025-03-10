using System;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace mROA.Implementation
{
    public interface ICallRequest
    {
        Guid Id { get; }
        int CommandId { get; }
        UniversalObjectIdentifier ObjectId { get; }
        object?[]? Parameters { get; }
    }

    public class DefaultCallRequest : ICallRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int CommandId { get; set; }
        public UniversalObjectIdentifier ObjectId { get; set; } = UniversalObjectIdentifier.Null;

        public object?[]? Parameters { get; set; }

        public override string ToString()
        {
            return $"Call request {{ Id : {Id}, CommandId : {CommandId}, ObjectId : {ObjectId} }}";
        }
    }

    public class CancelRequest : ICallRequest
    {
        public Guid Id { get; set; }
        public int CommandId { get; set; } = -2;
        public UniversalObjectIdentifier ObjectId { get; set; } = UniversalObjectIdentifier.Null;
        public object?[]? Parameters { get; set; } = null;

        public override string ToString()
        {
            return $"Cancel request {{ Id : {Id}, CommandId : {CommandId}, ObjectId : {ObjectId} }}";
        }
    }
}
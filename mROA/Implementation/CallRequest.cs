using System;

namespace mROA.Implementation
{
    public interface ICallRequest
    {
        RequestId Id { get; }
        int CommandId { get; }
        ComplexObjectIdentifier ObjectId { get; }
        object?[]? Parameters { get; }
    }

    public struct DefaultCallRequest : ICallRequest
    {
        public RequestId Id { get; set; }
        public int CommandId { get; set; }
        public ComplexObjectIdentifier ObjectId { get; set; }

        public object?[]? Parameters { get; set; }

        public override string ToString()
        {
            return $"Call request {{ Id : {Id}, CommandId : {CommandId}, ObjectId : {ObjectId} }}";
        }
    }

    public class CancelRequest : ICallRequest
    {
        public RequestId Id { get; set; }
        public int CommandId { get; set; } = -2;
        public ComplexObjectIdentifier ObjectId { get; set; } = ComplexObjectIdentifier.Null;
        public object?[]? Parameters { get; set; } = null;

        public override string ToString()
        {
            return $"Cancel request {{ Id : {Id}, CommandId : {CommandId}, ObjectId : {ObjectId} }}";
        }
    }
}
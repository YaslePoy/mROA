using System;
using mROA.Implementation.Attributes;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace mROA.Implementation
{
    public interface ICallRequest
    {
        Guid Id { get; }
        int CommandId { get; }
        int ObjectId { get; }
        object?[]? Parameters { get; }
    }

    public class DefaultCallRequest : ICallRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int CommandId { get; set; }
        public int ObjectId { get; set; } = -1;

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
        public int ObjectId { get; set; } = -2;
        public object?[]? Parameters { get; set; } = null;
        public override string ToString()
        {
            return $"Cancel request {{ Id : {Id}, CommandId : {CommandId}, ObjectId : {ObjectId} }}";
        }
    }
}
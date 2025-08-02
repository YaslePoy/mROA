using System;

namespace mROA.Implementation
{


    public struct CallRequest
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

    public struct CancelRequest
    {
        public RequestId Id { get; set; }
    }
}
using System;

namespace mROA.Implementation
{
    public sealed class RequestContext
    {
        public int OwnerId { get; }
        public Guid RequestId { get; }

        public RequestContext(Guid requestId, int ownerId)
        {
            RequestId = requestId;
            OwnerId = ownerId;
        }
    }
}
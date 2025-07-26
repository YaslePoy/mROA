using System;

namespace mROA.Implementation
{
    public struct RequestContext
    {
        public int OwnerId { get; }
        public RequestId RequestId { get; }

        public RequestContext(RequestId requestId, int ownerId)
        {
            RequestId = requestId;
            OwnerId = ownerId;
        }
    }
}
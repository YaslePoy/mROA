using System;

namespace mROA.Implementation
{
    public struct RequestId
    {
        public ulong P0;
        public ulong P1;
        public RequestId Generate()
        {
            var guid = Guid.NewGuid();
            var bytes = guid.ToByteArray();
            var high = BitConverter.ToUInt64(bytes, 0);
            var low = BitConverter.ToUInt64(bytes, 8);
            return new RequestId{ P0 = high, P1 = low};
        }
    }
}
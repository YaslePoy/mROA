using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace mROA.Implementation
{
    public struct RequestId : IEquatable<RequestId>
    {
        private static Random _random = new();

        public ulong P0;
        public ulong P1;

        public static RequestId Generate()
        {
            var high = (ulong)(ushort)_random.Next() << 32 | (ulong)_random.Next();
            var low = (ulong)(ushort)_random.Next() << 32 | (ulong)_random.Next();
            return new RequestId { P0 = high, P1 = low };
        }

        public RequestId(byte[] bytes)
        {
            P0 = BitConverter.ToUInt64(bytes);
            P1 = BitConverter.ToUInt64(bytes, 8);
        }

        public bool Equals(RequestId other)
        {
            return P0 == other.P0 && P1 == other.P1;
        }

        public override bool Equals(object? obj)
        {
            return obj is RequestId other && Equals(other);
        }

        public static bool operator ==(RequestId r1, RequestId r2)
        {
            return r1.Equals(r2);
        }

        public static bool operator !=(RequestId r1, RequestId r2)
        {
            return !(r1 == r2);
        }

        public override string ToString()
        {
            return $"{P0:X}{P1:X}";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(P0, P1);
        }

        public byte[] ToByteArray()
        {
            var array = new byte[16];
            MemoryMarshal.Write(array, ref this);
            return array;
        }
    }
}
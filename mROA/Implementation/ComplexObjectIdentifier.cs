using System;

namespace mROA.Implementation
{
    public struct ComplexObjectIdentifier : IEquatable<ComplexObjectIdentifier>
    {
        public int ContextId;
        public int OwnerId;

        public ComplexObjectIdentifier(int contextId, int ownerId)
        {
            ContextId = contextId;
            OwnerId = ownerId;
        }

        public static ComplexObjectIdentifier Null = new() { ContextId = -2, OwnerId = 0 };
        public static ComplexObjectIdentifier FromFlat(ulong flat) => new() { Flat = flat };


        public override string ToString()
        {
            return $"{{ {nameof(ContextId)}: {ContextId}, {nameof(OwnerId)}: {OwnerId} }}";
        }

        public bool IsStatic => ContextId == -1;

        public ulong Flat
        {
            get => (ulong)((long)OwnerId << 32 | (uint)ContextId);
            set
            {
                OwnerId = (int)(value >> 32);
                ContextId = (int)(value & 0xFFFFFFFF);
            }
        }

        public bool Equals(ComplexObjectIdentifier other)
        {
            return ContextId == other.ContextId && OwnerId == other.OwnerId;
        }

        public override bool Equals(object? obj)
        {
            return obj is ComplexObjectIdentifier other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ContextId, OwnerId);
        }
    }
}
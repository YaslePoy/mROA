using System;

namespace mROA.Implementation
{
#pragma warning disable CS8618, CS9264
    public struct UniversalObjectIdentifier : IEquatable<UniversalObjectIdentifier>
    {
        public int ContextId;
        public int OwnerId;

        public UniversalObjectIdentifier(int contextId, int ownerId)
        {
            ContextId = contextId;
            OwnerId = ownerId;
        }

        public static UniversalObjectIdentifier Null = new UniversalObjectIdentifier { ContextId = -2, OwnerId = -1 };
        public static UniversalObjectIdentifier FromFlat(ulong flat) => new() { Flat = flat };

        public override string ToString()
        {
            return $"{{ {nameof(ContextId)}: {ContextId}, {nameof(OwnerId)}: {OwnerId} }}";
        }

        public bool IsStatic => ContextId == -1;

        public ulong Flat
        {
            get => (ulong)OwnerId << 32 | (uint)ContextId;
            set
            {
                OwnerId = (int)(value >> 32);
                ContextId = (int)(value & 0xFFFFFFFF);
            }
        }

        public bool Equals(UniversalObjectIdentifier other)
        {
            return ContextId == other.ContextId && OwnerId == other.OwnerId;
        }

        public override bool Equals(object? obj)
        {
            return obj is UniversalObjectIdentifier other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ContextId, OwnerId);
        }
    }
}
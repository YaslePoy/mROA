using System;
using System.Formats.Cbor;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using mROA.Implementation;

namespace mROA.Cbor
{
    public static class CborExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteToCborInline(this RequestId id, CborWriter writer)
        {
            Span<byte> span = stackalloc byte[16];
            MemoryMarshal.Write(span, ref id);
            writer.WriteByteString(span);
        }
    }
}
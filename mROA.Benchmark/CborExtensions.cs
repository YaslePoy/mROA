using System.Formats.Cbor;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using mROA.Implementation;

public static class CborExtensions
{
    public static unsafe void WriteToCbor(this RequestId id, CborWriter writer)
    {
        Span<byte> span = stackalloc byte[16];
        MemoryMarshal.Write(span, ref id);
        writer.WriteByteString(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void WriteToCborInline(this RequestId id, CborWriter writer)
    {
        Span<byte> span = stackalloc byte[16];
        MemoryMarshal.Write(span, ref id);
        writer.WriteByteString(span);
    }
    
    public static void WriteToDest(this RequestId id, Span<byte> destination)
    {
        MemoryMarshal.Write(destination, ref id);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe void WriteToCborOpt(this RequestId id, CborWriter writer)
    {
        Span<byte> span = stackalloc byte[16];
        MemoryMarshal.Write(span, ref id);
        writer.WriteByteString(span);
    }
}
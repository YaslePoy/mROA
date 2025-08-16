using System.Formats.Cbor;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using mROA.Implementation;

namespace mROA.Benchmark;

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RequestId ReadFromTrueCbor(CborReader reader)
    {
        var enc = reader.ReadEncodedValue(true);
        return MemoryMarshal.Read<RequestId>(enc.Span[1..]);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RequestId ReadFromCbor(CborReader reader)
    {
        var enc = reader.ReadEncodedValue();
        return MemoryMarshal.Read<RequestId>(enc.Span[1..]);
    }
}
// See https://aka.ms/new-console-template for more information

using System.Formats.Cbor;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Running;
using mROA.Benchmark;
using mROA.Implementation;

Console.WriteLine("Hello, World!");
var test = new CborTest();
test.ArrayWrite();
BenchmarkRunner.Run<VirtualOverhead>();

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
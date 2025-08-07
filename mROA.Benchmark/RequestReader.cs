using System.Formats.Cbor;
using BenchmarkDotNet.Attributes;
using mROA.Implementation;

namespace mROA.Benchmark;

[MemoryDiagnoser]
[DisassemblyDiagnoser]
public class RequestReader
{
    private ReadOnlyMemory<byte> _data;

    public RequestReader()
    {
        _data = new ReadOnlyMemory<byte>([80, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16]);
    }

    [Benchmark(Baseline = true)]
    public ulong DefaultRead()
    {
        var reader = new CborReader(_data);
        return new RequestId(reader.ReadByteString()).P0;
    }
    
    [Benchmark]
    public ulong MemoryRead()
    {
        var reader = new CborReader(_data);
        return CborExtensions.ReadFromCbor(reader).P0;
    }
    
    [Benchmark]
    public ulong MemoryTrueRead()
    {
        var reader = new CborReader(_data);
        return CborExtensions.ReadFromTrueCbor(reader).P0;
    }
}
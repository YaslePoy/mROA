using System.Formats.Cbor;
using BenchmarkDotNet.Attributes;
using mROA.Implementation;

namespace mROA.Benchmark;

[MemoryDiagnoser]
public class RequestWriter
{
    public RequestId Id;
    private readonly CborWriter _writer;

    public RequestWriter()
    {
        _writer = new CborWriter(initialCapacity: 512);
    }
        
    [Benchmark(Baseline = true)]
    public int DefaultCbor()
    {
        _writer.Reset();
        _writer.WriteByteString(Id.ToByteArray());
        return _writer.BytesWritten;
    }
    [Benchmark]
    public int DirectCbor()
    {
        _writer.Reset();
        Id.WriteToCbor(_writer);
        return _writer.BytesWritten;
    }
    
    [Benchmark]
    public int Stackalloc()
    {
        _writer.Reset();
        Span<byte> span = stackalloc byte[16];
        Id.WriteToDest(span);
        _writer.WriteByteString(span);
        return _writer.BytesWritten;
    }
    
    [Benchmark]
    public int DirectCborInline()
    {
        _writer.Reset();
        Id.WriteToCborInline(_writer);
        return _writer.BytesWritten;
    }
    
    [Benchmark]
    public int DirectCborOpt()
    {
        _writer.Reset();
        Id.WriteToCborOpt(_writer);
        return _writer.BytesWritten;
    }
}
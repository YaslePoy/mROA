using System.Formats.Cbor;
using BenchmarkDotNet.Attributes;

namespace mROA.Benchmark;


[MemoryDiagnoser]
public class CborTest
{
    private CborWriter _writer;
    private CborReader _reader;

    private Memory<byte> FlatEncoded;
    private Memory<byte> ArrayEncoded;
    public CborTest()
    {
        _writer = new CborWriter(initialCapacity:512);
        
        _writer.WriteStartArray(2);
        _writer.WriteByteString([1, 2, 3, 4, 5, 6, 7, 8]);
        _writer.WriteStartArray(2);
        _writer.WriteInt32(12);
        _writer.WriteTextString("tralala");
        _writer.WriteEndArray();
        _writer.WriteEndArray();
        
        ArrayEncoded = _writer.Encode();
        
        _writer.Reset();
        
        _writer.WriteStartArray(3);
        _writer.WriteByteString([1, 2, 3, 4, 5, 6, 7, 8]);
        _writer.WriteInt32(12);
        _writer.WriteTextString("tralala");
        _writer.WriteEndArray();
        FlatEncoded = _writer.Encode();
    }
    
    [Benchmark]
    public int FlatWrite()
    {
        _writer.Reset();
        _writer.WriteStartArray(3);
        _writer.WriteByteString([1, 2, 3, 4, 5, 6, 7, 8]);
        _writer.WriteInt32(12);
        _writer.WriteTextString("tralala");
        _writer.WriteEndArray();
        var len = _writer.Encode(FlatEncoded.Span);
        return len;
    }
    
    [Benchmark]
    public int ArrayWrite()
    {
        _writer.Reset();
        _writer.WriteStartArray(2);
        _writer.WriteByteString([1, 2, 3, 4, 5, 6, 7, 8]);
        _writer.WriteStartArray(2);
        _writer.WriteInt32(12);
        _writer.WriteTextString("tralala");
        _writer.WriteEndArray();
        _writer.WriteEndArray();
        var len = _writer.Encode(ArrayEncoded.Span);
        return len;
    }
    [Benchmark]
    public int FlatRead()
    {
        var reader = new CborReader(FlatEncoded);
        reader.ReadStartArray();
        var arr = reader.ReadByteString();
        var i = reader.ReadInt32();
        var text = reader.ReadTextString();
        reader.ReadEndArray();
        
        return arr.Length;
    }
    
    [Benchmark]
    public int ArrayRead()
    {
        var reader = new CborReader(ArrayEncoded);
        reader.ReadStartArray();
        var arr = reader.ReadByteString();
        reader.ReadStartArray();
        var i = reader.ReadInt32();
        var text = reader.ReadTextString();
        reader.ReadEndArray();
        reader.ReadEndArray();
        
        return arr.Length;
    }
}
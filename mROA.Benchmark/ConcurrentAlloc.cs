using System.Formats.Cbor;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using mROA.Implementation;

namespace mROA.Benchmark;

[MemoryDiagnoser]
public class ConcurrentAlloc
{
    private readonly CircularMemoryManager _cmm = new(1024);
    private readonly FastCircularMemoryManager _fmm = new();
    private CborWriter _writer = new();

    [GlobalSetup]
    public void Setup()
    {
        _writer.WriteStartArray(2);
        _writer.WriteByteString([1, 2, 3, 4, 5, 6, 7, 8]);
        _writer.WriteStartArray(2);
        _writer.WriteInt32(12);
        _writer.WriteTextString("tralala 7EBC1458-BB53-49EA-84C9-EFECC0FC08FD");
        _writer.WriteEndArray();
        _writer.WriteEndArray();
    }
    
    [Benchmark(Baseline = true)]
    public int CircularAllocate()
    {
        var writer = _writer;
        var buffer = _cmm.AllocSlice(writer.BytesWritten);
        writer.Encode(buffer);
        
        return buffer.Length;
    }
    
    [Benchmark]
    public int CircularMemoryAllocate()
    {
        var writer = _writer;
        var buffer = _cmm.AllocMemory(writer.BytesWritten);
        writer.Encode(buffer.Span);
        return buffer.Length;
    }
    
    [Benchmark]
    public int FastCircularAllocate()
    {
        var writer = _writer;
        var buffer = _fmm.Alloc(writer.BytesWritten);
        writer.Encode(buffer);
        return buffer.Length;
    }
    
    [Benchmark]
    public int FastMemoryCircularAllocate()
    {
        var writer = _writer;
        var buffer = _fmm.AllocMem(writer.BytesWritten);
        writer.Encode(buffer.Span);
        return buffer.Length;
    }
    
    [Benchmark]
    public int HeapAllocate()
    {
        var writer = _writer;
        var encoded = writer.Encode();
        return encoded.Length;
    }
}

public class FastCircularMemoryManager
{
    private readonly byte[] _buffer;
    private long _offset; // atomic offset (in bytes)
    private readonly int _mask; // если размер степени двойки — можно использовать маску

    public FastCircularMemoryManager(int size = 4096) // 4KB buffer
    {
        if (!IsPowerOfTwo(size))
            throw new ArgumentException("Size should be power of two for performance.", nameof(size));

        _buffer = new byte[size];
        _offset = 0;
        _mask = size - 1; // для быстрого циклического сдвига: (offset & _mask)
    }

    private static bool IsPowerOfTwo(int x) => x > 0 && (x & (x - 1)) == 0;

    public Span<byte> Alloc(int size)
    {
        if (size > _buffer.Length)
            return new byte[size]; // fallback

        long oldOffset, newOffset;
        int start;

        // Atomic "bump pointer" с циклическим переполнением
        do
        {
            oldOffset = Volatile.Read(ref _offset);
            start = (int)(oldOffset & _mask);

            // Проверяем, не пересекает ли выделение границу буфера
            if (start + size > _buffer.Length)
            {
                // Переполнение — обнуляем (циклический буфер)
                newOffset = size; // сбрасываем на начало + size
                start = 0;
            }
            else
            {
                newOffset = oldOffset + size;
            }
        } while (Interlocked.CompareExchange(ref _offset, newOffset, oldOffset) != oldOffset);

        return _buffer.AsSpan(start, size);
    }
    
    public Memory<byte> AllocMem(int size)
    {
        if (size > _buffer.Length)
            return new byte[size]; // fallback

        long oldOffset, newOffset;
        int start;

        // Atomic "bump pointer" с циклическим переполнением
        do
        {
            oldOffset = Volatile.Read(ref _offset);
            start = (int)(oldOffset & _mask);

            // Проверяем, не пересекает ли выделение границу буфера
            if (start + size > _buffer.Length)
            {
                // Переполнение — обнуляем (циклический буфер)
                newOffset = size; // сбрасываем на начало + size
                start = 0;
            }
            else
            {
                newOffset = oldOffset + size;
            }
        } while (Interlocked.CompareExchange(ref _offset, newOffset, oldOffset) != oldOffset);

        return _buffer.AsMemory(start, size);
    }
}
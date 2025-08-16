using BenchmarkDotNet.Attributes;
using mROA.Implementation;

namespace mROA.Benchmark;

public class StackOperations
{
    private Stack<StackFrame> stack = new();
    
    
    
    public StackOperations()
    {
        
    }

    [Benchmark]
    public void Stack()
    {
        
    }
    private readonly struct StackFrame
    {
        public StackFrame(
            CborMajorType? type,
            int frameOffset,
            int? definiteLength,
            int itemsWritten,
            int? currentKeyOffset,
            int? currentValueOffset,
            List<int>? keyValuePairEncodingRanges,
            HashSet<(int Offset, int Length)>? keyEncodingRanges)
        {
            MajorType = type;
            FrameOffset = frameOffset;
            DefiniteLength = definiteLength;
            ItemsWritten = itemsWritten;
            CurrentKeyOffset = currentKeyOffset;
            CurrentValueOffset = currentValueOffset;
            KeyValuePairEncodingRanges = keyValuePairEncodingRanges;
            KeyEncodingRanges = keyEncodingRanges;
        }

        public CborMajorType? MajorType { get; }
        public int FrameOffset { get; }
        public int? DefiniteLength { get; }
        public int ItemsWritten { get; }

        public int? CurrentKeyOffset { get; }
        public int? CurrentValueOffset { get; }
        public List<int>? KeyValuePairEncodingRanges { get; }
        public HashSet<(int Offset, int Length)>? KeyEncodingRanges { get; }
    }
}

internal enum CborMajorType
{
    Unknown,
    Int8,
    Int16,
    Int32,
    Int64,
    UInt8,
    UInt16,
    UInt32,
    UInt64,
    Float32,
    Float64,
    Double,
    Double2,
    Double3,
}


using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace mROA.Benchmark;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        var summary = BenchmarkRunner.Run<CollectionsSpeed>();

    }
}

public class CollectionsSpeed
{
    private const int N = 1000;
    
    private readonly List<int> _immutable;
    private readonly int[] _array;

    public CollectionsSpeed()
    {
        _array = Enumerable.Range(0, N).ToArray();
        _immutable = [.._array];
    }

    [Benchmark]
    public int DefaultArray()
    {
        var sum = 0;
        for (int i = 0; i < N; i++)
        {
            sum += _array[i];
        }
        return sum;
    }
    
    [Benchmark]
    public int ImmutableArray()
    {
        var sum = 0;
        for (int i = 0; i < N; i++)
        {
            sum += _immutable[i];
        }
        return sum;
    }
}
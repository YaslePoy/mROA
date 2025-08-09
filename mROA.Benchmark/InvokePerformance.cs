using BenchmarkDotNet.Attributes;

namespace mROA.Benchmark;

[MemoryDiagnoser]
[DisassemblyDiagnoser]
public class InvokePerformance
{
    public Func<int, int> A = x =>
    {
        var result = x * 5 + x * x / 5;
        return result;
    };
    
    public SomeMath B = x => x * 5 + x * x / 5; 
    
    [Benchmark(Baseline = true)]
    public int ActionInvoke()
    {
        return A(132);
    }
    
    [Benchmark]
    public int DelegateInvoke()
    {
        return A(132);
    }
}

public delegate int SomeMath(int x);
using BenchmarkDotNet.Attributes;
using mROA.Abstract;
using mROA.Codegen;
using mROA.Implementation;

namespace mROA.Benchmark;

[MemoryDiagnoser]
[DisassemblyDiagnoser]
public class MethodAccess
{
    private CollectableMethodRepository _current =  new();
    private FastMethodRepository _fast = new();
    private readonly int _count = new GeneratedInvokersCollection().Count;
    [GlobalSetup]
    public void SetupMethodAccess()
    {
        _current.AppendInvokers(new GeneratedInvokersCollection());
        _fast.AppendInvokers(new GeneratedInvokersCollection());
    }

    // [Benchmark(Baseline = true)]
    // public IMethodInvoker CurrentSingle()
    // {
    //     return _current.GetMethod(0);
    // }
    //
    // [Benchmark]
    // public IMethodInvoker CurrentDispose()
    // {
    //     return _current.GetMethod(-1);
    // }
    //
    // [Benchmark]
    // public IMethodInvoker FastSingle()
    // {
    //     return _fast.GetMethod(0);
    // }
    //
    // [Benchmark]
    // public IMethodInvoker FastDispose()
    // {
    //     return _fast.GetMethod(-1);
    // }
    // [Benchmark]
    // public IMethodInvoker FastSingleBaked()
    // {
    //     return _fast.GetMethodBaked(0);
    // }
    //
    // [Benchmark]
    // public IMethodInvoker FastDisposeBaked()
    // {
    //     return _fast.GetMethodBaked(-1);
    // }
    
    
    [Benchmark(Baseline = true)]
    public IMethodInvoker CurrentAll()
    {
        IMethodInvoker inv = null;
        for (int i = -1; i < _count; i++)
        {
            inv = _current.GetMethod(i);
        }
        return inv;
    }
    
    [Benchmark]
    public IMethodInvoker FastAll()
    {
        IMethodInvoker inv = null;
        for (int i = -1; i < _count; i++)
        {
            inv = _fast.GetMethod(i);
        }
        return inv;
    }
    
    [Benchmark]
    public IMethodInvoker FastAllBaked()
    {
        IMethodInvoker inv = null;
        for (int i = -1; i < _count; i++)
        {
            inv = _fast.GetMethodBaked(i);
        }
        return inv;
    }
}

internal class FastMethodRepository: IMethodRepository
{
    private readonly List<IMethodInvoker> _methods = [MethodInvoker.Dispose];
    private IMethodInvoker[] _baked = [];
    public void AppendInvokers(IEnumerable<IMethodInvoker> methodInvokers)
    {
        _methods.AddRange(methodInvokers);
        _baked = _methods.ToArray();
    }
    public IMethodInvoker GetMethod(int id)
    {
        return _methods[id + 1];
    }
    
    public IMethodInvoker GetMethodBaked(int id)
    {
        return _baked[id + 1];
    }
}
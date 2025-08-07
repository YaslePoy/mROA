using System;
using System.Threading.Tasks;
using mROA.Implementation;

namespace mROA.Test;

[TestFixture]
public class ConcurrentTest
{
    private CircularMemoryManager _cmm;
    
    [SetUp]
    public void Setup()
    {
        _cmm = new CircularMemoryManager(100);
    }

    [Test]
    public void ParallelAlloc()
    {
        Parallel.For(0, 100, i =>
        {
            var data = _cmm.AllocSlice(1);
            data.Span[0] = 1;
        });

        var total = _cmm.AllocSlice(100).Span;
        if (total.IndexOf((byte)0) == -1)
        {
            Assert.Pass();
        }
        Assert.Fail();
    }
}
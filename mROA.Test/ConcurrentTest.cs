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
        Assert.Fail();
    }
}
using mROA.Benchmark;

namespace mROA.Test;

[TestFixture]
public class BenchmarkTest
{

    [Test]
    public void Benchmark()
    {
        var bench = new TaskWaiting();
        var x = bench.DefaultJob();
        var y = bench.DefaultJobAsync();
        y.Wait();
        if (x == y.Result)
        {
            Assert.Pass();
        }
        Assert.Fail();
    }
}
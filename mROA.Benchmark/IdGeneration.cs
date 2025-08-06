using BenchmarkDotNet.Attributes;
using mROA.Implementation;

namespace mROA.Benchmark;

public class IdGeneration
{
    public static int X = 0;

    [Benchmark]
    public Guid GuidGeneration()
    {
        return Guid.NewGuid();
    }

    [Benchmark(Baseline = true)]
    public RequestId ReqIdGeneration()
    {
        return RequestId.Generate();
    }

    [Benchmark]
    public RequestId ReqIdIfGeneration()
    {
        var reqId = RequestId.Generate();
        if (++X % 2 == 0)
        {
            reqId.P0 = 0;
        }
        else
        {
            reqId.P1 = 0;
        }
        return reqId;
        
    }
}
using BenchmarkDotNet.Attributes;
using mROA.Implementation;

namespace mROA.Benchmark;

public class VirtualOverhead
{
    private ICallRequest _request;
    private CallRequest _directRequest;
    private RawCallRequest _rawRequest;

    public VirtualOverhead()
    {
        _request = new CallRequest
        {
            CommandId = 5, Id = new RequestId(), ObjectId = ComplexObjectIdentifier.Null, Parameters = null
        };
        _directRequest = (CallRequest)_request;
        _rawRequest = new RawCallRequest
        {
            CommandId = _directRequest.CommandId, Id = _directRequest.Id, ObjectId = _directRequest.ObjectId,
            Parameters = null
        };
    }

    [Benchmark(Baseline = true)]
    public long VirtualUsage()
    {
        var req = _request;
        long acc = 0;
        acc += req.CommandId;
        acc += (long)(req.Id.P1 + req.Id.P0);
        acc += req.ObjectId.ContextId + req.ObjectId.OwnerId;
        acc += (req.Parameters ?? []).Length;
        return acc;
    }

    [Benchmark]
    public long DirectUsage()
    {
        var req = _directRequest;
        long acc = 0;
        acc += req.CommandId;
        acc += (long)(req.Id.P1 + req.Id.P0);
        acc += req.ObjectId.ContextId + req.ObjectId.OwnerId;
        acc += (req.Parameters ?? []).Length;
        return acc;
    }

    [Benchmark]
    public long RawUsage()
    {
        var req = _rawRequest;
        long acc = 0;
        acc += req.CommandId;
        acc += (long)(req.Id.P1 + req.Id.P0);
        acc += req.ObjectId.ContextId + req.ObjectId.OwnerId;
        acc += (req.Parameters ?? []).Length;
        return acc;
    }
}

public struct RawCallRequest
{
    public RequestId Id;
    public int CommandId;
    public ComplexObjectIdentifier ObjectId;

    public object?[]? Parameters;

    public override string ToString()
    {
        return $"Call request {{ Id : {Id}, CommandId : {CommandId}, ObjectId : {ObjectId} }}";
    }
}
using BenchmarkDotNet.Attributes;
using mROA.Abstract;
using mROA.Cbor;
using mROA.Implementation;
using mROA.Implementation.Attributes;
using mROA.Implementation.Backend;
using mROA.Implementation.CommandExecution;

namespace mROA.Benchmark;

public class TaskWaiting
{
    private readonly BasicExecutionModule _executionModule;
    private readonly CallRequest _syncRequest;
    private readonly CallRequest _asyncRequest;
    private readonly InstanceRepository _instanceRepo;
    private readonly EndPointContext _endPointContext;
    private readonly FastRepresentationModule _representationModule;
    public TaskWaiting()
    {
        _executionModule = new BasicExecutionModule(new CancellationRepository(),  new TestMethodRepo(), new CborSerializationToolkit());
        _syncRequest = new CallRequest{CommandId = 0, Parameters = null, Id = RequestId.Generate(), ObjectId = new ComplexObjectIdentifier(-1, 0)};
        _asyncRequest = new CallRequest{CommandId = 1, Parameters = null, Id = RequestId.Generate(), ObjectId = new ComplexObjectIdentifier(-1, 0)};
        _instanceRepo = new InstanceRepository(null);
        _instanceRepo.FillSingletons(typeof(TaskWaiting).Assembly);
        _endPointContext = new EndPointContext(_instanceRepo, null)
        {
            OwnerId = 0
        };
        _representationModule = new FastRepresentationModule();
    }

    [Benchmark(Baseline = true)]
    public int DefaultJob()
    {
        return (int)((FinalCommandExecution<object>)_executionModule.Execute(_syncRequest, _instanceRepo, _representationModule, _endPointContext)).Result;
    }

    [Benchmark]
    public async Task<int> DefaultJobAsync()
    {
        _representationModule.Signal = new TaskCompletionSource<int>();
        var task = _representationModule.Signal.Task;
        _ = _executionModule.Execute(_asyncRequest, _instanceRepo, _representationModule, _endPointContext);
        _ = await task;
        return (int)((FinalCommandExecution<object>)_representationModule.Result).Result;
    }
}

public class TestMethodRepo : IMethodRepository
{
    public IMethodInvoker GetMethod(int id)
    {
        if (id == 0)
            return new MethodInvoker
            {
                IsVoid = false,
                IsTrusted = true,
                ReturnType = typeof(int),
                ParameterTypes = Type.EmptyTypes,
                SuitableType = typeof(IJobClass),
                Invoking = (i, _, _) => (i as IJobClass).A()
            };
        
        return new AsyncMethodInvoker
        {
            IsVoid = false,
            IsTrusted = true,
            ReturnType = typeof(int),
            ParameterTypes = Type.EmptyTypes,
            SuitableType = typeof(IJobClass),
            Invoking = (i, _, _, post) => (i as IJobClass).B().ContinueWith(task => post(task.Result))
        };
    }
}

[SharedObjectInterface]
public interface IJobClass
{
    int A();
    Task<int> B();
}

[SharedObjectSingleton]
public class JobClass : IJobClass
{
    private readonly RequestWriter _requestWriter =  new();
    public int A()
    {
        return _requestWriter.DefaultCbor();
    }

    public Task<int> B()
    {
        return Task.FromResult(_requestWriter.DefaultCbor());
    }
}

public class FastRepresentationModule : IRepresentationModule
{
    public TaskCompletionSource<int> Signal = new();
    
    public object Result;
    public int Id { get; }
    public IEndPointContext Context { get; }
    public Task<(object? Deserialized, EMessageType MessageType)> GetSingle(Predicate<NetworkMessage> rule, IEndPointContext? context, CancellationToken token = default, params Func<NetworkMessage, Type?>[] converter)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<(object parced, EMessageType originalType)> GetStream(Predicate<NetworkMessage> rule, IEndPointContext? context, CancellationToken token = default,
        params Func<NetworkMessage, Type?>[] converter)
    {
        throw new NotImplementedException();
    }

    public Task PostCallMessageAsync<T>(RequestId id, EMessageType eMessageType, T payload, IEndPointContext? context) where T : notnull
    {
        Result = payload;
        Signal.SetResult(0);
        return Task.CompletedTask;
    }

    public void PostCallMessage<T>(RequestId id, EMessageType eMessageType, T payload, IEndPointContext? context) where T : notnull
    {
        throw new NotImplementedException();
    }

    public Task PostCallMessageUntrustedAsync<T>(RequestId id, EMessageType eMessageType, T payload, IEndPointContext? context) where T : notnull
    {
        throw new NotImplementedException();
    }
}
using mROA.Abstract;
using mROA.Implementation.CommandExecution;

// ReSharper disable UnusedMember.Global

namespace mROA.Implementation;

public abstract class RemoteObjectBase(int id, IRepresentationModule representationModule)
{
    public int Id => id;
    public int OwnerId => representationModule.Id;

    protected async Task<T> GetResultAsync<T>(int methodId, object? parameter = default)
    {
        var request = new DefaultCallRequest
            { CommandId = methodId, ObjectId = id, Parameter = parameter, ParameterType = parameter?.GetType() };
        await representationModule.PostCallMessageAsync(request.Id, EMessageType.CallRequest, request);

        var localTokenSource = new CancellationTokenSource();

        var successResponse =
            representationModule.GetMessageAsync<FinalCommandExecution<T>>(request.Id,
                EMessageType.FinishedCommandExecution, localTokenSource.Token);
        
        var errorResponse =
            representationModule.GetMessageAsync<ExceptionCommandExecution>(request.Id,
                EMessageType.ExceptionCommandExecution, localTokenSource.Token);

        Task.WaitAny(successResponse, errorResponse);
        
        if (successResponse.IsCompletedSuccessfully)
        {
            await localTokenSource.CancelAsync();
            return successResponse.Result.Result!;
        }

        await localTokenSource.CancelAsync();
        throw errorResponse.Result.GetException();
    }

    protected async Task CallAsync(int methodId, object? parameter = default)
    {
        var request = new DefaultCallRequest
            { CommandId = methodId, ObjectId = id, Parameter = parameter, ParameterType = parameter?.GetType() };
        await representationModule.PostCallMessageAsync(request.Id, EMessageType.CallRequest, request);

        var successResponse =
            representationModule.GetMessageAsync<FinalCommandExecution>(
                messageType: EMessageType.FinishedCommandExecution, requestId: request.Id);
        var errorResponse =
            representationModule.GetMessageAsync<ExceptionCommandExecution>(
                messageType: EMessageType.ExceptionCommandExecution, requestId: request.Id);

        Task.WaitAny(successResponse, errorResponse);

        if (successResponse.IsCompletedSuccessfully)
            return;

        throw errorResponse.Result.GetException();
    }
}
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
        await representationModule.PostCallMessageAsync(request.Id, MessageType.CallRequest, request);

        var successResponse =
            representationModule.GetMessageAsync<FinalCommandExecution<T>>(
                messageType: MessageType.FinishedCommandExecution, requestId: request.Id);
        var errorResponse =
            representationModule.GetMessageAsync<ExceptionCommandExecution>(
                messageType: MessageType.ExceptionCommandExecution, requestId: request.Id);
        Task.WaitAny(successResponse, errorResponse);

        if (successResponse.IsCompletedSuccessfully)
            return successResponse.Result.Result!;

        throw errorResponse.Result.GetException();
    }

    protected async Task CallAsync(int methodId, object? parameter = default)
    {
        var request = new DefaultCallRequest
            { CommandId = methodId, ObjectId = id, Parameter = parameter, ParameterType = parameter?.GetType() };
        await representationModule.PostCallMessageAsync(request.Id, MessageType.CallRequest, request);

        var successResponse =
            representationModule.GetMessageAsync<FinalCommandExecution>(
                messageType: MessageType.FinishedCommandExecution, requestId: request.Id);
        var errorResponse =
            representationModule.GetMessageAsync<ExceptionCommandExecution>(
                messageType: MessageType.ExceptionCommandExecution, requestId: request.Id);

        Task.WaitAny(successResponse, errorResponse);

        if (successResponse.IsCompletedSuccessfully)
            return;

        throw errorResponse.Result.GetException();
    }
}
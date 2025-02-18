using mROA.Implementation;
// ReSharper disable UnusedMember.Global

namespace mROA.Abstract;

public abstract class RemoteObjectBase
{
    private readonly int _id;
    private readonly IRepresentationModule _representationModule;
    public int Id => _id;
    public int OwnerId => _representationModule.Id;

    public RemoteObjectBase(int id, IRepresentationModule representationModule)
    {
        _id = id;
        _representationModule = representationModule;
    }

    public async Task<T> GetResultAsync<T>(int methodId, object? parameter = default)
    {
        var request = new DefaultCallRequest
            { CommandId = methodId, ObjectId = _id, Parameter = parameter, ParameterType = parameter?.GetType() };
        await _representationModule.PostCallMessageAsync(request.Id, MessageType.CallRequest, request);

        var successResponse =
            _representationModule.GetMessageAsync<FinalCommandExecution<T>>(
                messageType: MessageType.FinishedCommandExecution, requestId: request.Id);
        var errorResponse =
            _representationModule.GetMessageAsync<ExceptionCommandExecution>(
                messageType: MessageType.ExceptionCommandExecution, requestId: request.Id);
        Task.WaitAny(successResponse, errorResponse);

        if (successResponse.IsCompletedSuccessfully)
            return successResponse.Result.Result!;

        throw errorResponse.Result.GetException();
    }

    public async Task CallAsync(int methodId, object? parameter = default)
    {
        var request = new DefaultCallRequest
            { CommandId = methodId, ObjectId = _id, Parameter = parameter, ParameterType = parameter?.GetType() };
        await _representationModule.PostCallMessageAsync(request.Id, MessageType.CallRequest, request);

        var successResponse =
            _representationModule.GetMessageAsync<FinalCommandExecution>(
                messageType: MessageType.FinishedCommandExecution, requestId: request.Id);
        var errorResponse =
            _representationModule.GetMessageAsync<ExceptionCommandExecution>(
                messageType: MessageType.ExceptionCommandExecution, requestId: request.Id);

        Task.WaitAny(successResponse, errorResponse);

        if (successResponse.IsCompletedSuccessfully)
            return;

        throw errorResponse.Result.GetException();
    }
}
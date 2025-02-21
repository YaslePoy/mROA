using System.Threading;
using System.Threading.Tasks;
using mROA.Abstract;
using mROA.Implementation.CommandExecution;

// ReSharper disable UnusedMember.Global

namespace mROA.Implementation
{
    public abstract class RemoteObjectBase
    {
        private readonly int _id;
        private readonly IRepresentationModule _representationModule;

        protected RemoteObjectBase(int id, IRepresentationModule representationModule)
        {
            _id = id;
            _representationModule = representationModule;
        }

        public int Id => _id;
        public int OwnerId => _representationModule.Id;
        
        protected async Task<T> GetResultAsync<T>(int methodId, object? parameter = default,
            CancellationToken cancellationToken = default)
        {
            var request = new DefaultCallRequest
                { CommandId = methodId, ObjectId = _id, Parameter = parameter, ParameterType = parameter?.GetType() };
            await _representationModule.PostCallMessageAsync(request.Id, MessageType.CallRequest, request);

            var localTokenSource = new CancellationTokenSource();

            var successResponse =
                _representationModule.GetMessageAsync<FinalCommandExecution<T>>(request.Id,
                    MessageType.FinishedCommandExecution,
                    localTokenSource.Token);
            var errorResponse =
                _representationModule.GetMessageAsync<ExceptionCommandExecution>(requestId: request.Id,
                    MessageType.ExceptionCommandExecution, localTokenSource.Token);

            Task.WaitAny(new Task[]
            {
                successResponse, errorResponse
            }, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                await _representationModule.PostCallMessageAsync(request.Id, MessageType.CancelRequest, request.Id);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (successResponse.IsCompletedSuccessfully)
            {
                localTokenSource.Cancel();
                return successResponse.Result.Result!;
            }

            localTokenSource.Cancel();
            throw errorResponse.Result.GetException();
        }

        protected async Task CallAsync(int methodId, object? parameter = default,
            CancellationToken cancellationToken = default)
        {
            var request = new DefaultCallRequest
                { CommandId = methodId, ObjectId = _id, Parameter = parameter, ParameterType = parameter?.GetType() };
            await _representationModule.PostCallMessageAsync(request.Id, MessageType.CallRequest, request);

            var localTokenSource = new CancellationTokenSource();

            var successResponse =
                _representationModule.GetMessageAsync<FinalCommandExecution>(request.Id,
                    MessageType.FinishedCommandExecution,
                    localTokenSource.Token);
            var errorResponse =
                _representationModule.GetMessageAsync<ExceptionCommandExecution>(requestId: request.Id,
                    MessageType.ExceptionCommandExecution, localTokenSource.Token);

            Task.WaitAny(new Task[]
            {
                successResponse, errorResponse
            }, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                await _representationModule.PostCallMessageAsync(request.Id, MessageType.CancelRequest, request.Id);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (successResponse.IsCompletedSuccessfully)
                return;

            throw errorResponse.Result.GetException();
        }
    }
}
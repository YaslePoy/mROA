using System;
using System.Threading;
using System.Threading.Tasks;
using mROA.Abstract;
using mROA.Implementation.CommandExecution;

// ReSharper disable UnusedMember.Global

namespace mROA.Implementation
{
    public abstract class RemoteObjectBase : IDisposable
    {
        private readonly ComplexObjectIdentifier _identifier;
        private readonly IRepresentationModule _representationModule;

        protected RemoteObjectBase(int id, IRepresentationModule representationModule)
        {
            _identifier = new ComplexObjectIdentifier { ContextId = id, OwnerId = representationModule.Id };
            _representationModule = representationModule;
        }

        public int Id => _identifier.ContextId;
        public int OwnerId => _identifier.OwnerId;
        public ComplexObjectIdentifier Identifier => _identifier;

        public void Dispose()
        {
            if (_identifier.IsStatic)
                return;
            CallAsync(-1).Wait();
        }

        protected async Task<T> GetResultAsync<T>(int methodId, object?[]? parameters = null,
            CancellationToken cancellationToken = default)
        {
            var request = new DefaultCallRequest
            {
                CommandId = methodId, ObjectId = _identifier, Parameters = parameters
            };

            await _representationModule.PostCallMessageAsync(request.Id, MessageType.CallRequest, request);

            var localTokenSource = new CancellationTokenSource();

            var successResponse =
                _representationModule.GetMessageAsync<FinalCommandExecution<T>>(request.Id,
                    MessageType.FinishedCommandExecution,
                    localTokenSource.Token);
            var errorResponse =
                _representationModule.GetMessageAsync<ExceptionCommandExecution>(requestId: request.Id,
                    MessageType.ExceptionCommandExecution, localTokenSource.Token);

            cancellationToken.Register(async () =>
            {
#if TRACE
                Console.WriteLine("Cancelling task");
#endif
                await _representationModule.PostCallMessageAsync(request.Id, MessageType.CancelRequest,
                    new CancelRequest
                    {
                        Id = request.Id
                    });
                localTokenSource.Cancel();
            });

            Task.WaitAny(new Task[]
            {
                successResponse, errorResponse
            }, cancellationToken);

            // if (cancellationToken.IsCancellationRequested)
            // {
            //     await _representationModule.PostCallMessageAsync(request.Id, MessageType.CancelRequest, request.Id);
            //     localTokenSource.Cancel();
            //     cancellationToken.ThrowIfCancellationRequested();
            // }

            if (successResponse.IsCompletedSuccessfully)
            {
                localTokenSource.Cancel();
                return successResponse.Result.Result!;
            }

            localTokenSource.Cancel();
            throw errorResponse.Result.GetException();
        }

        protected async Task CallAsync(int methodId, object?[]? parameters = null,
            CancellationToken cancellationToken = default)
        {
            var request = new DefaultCallRequest
            {
                CommandId = methodId, ObjectId = _identifier, Parameters = parameters
            };
            await _representationModule.PostCallMessageAsync(request.Id, MessageType.CallRequest, request);

            var localTokenSource = new CancellationTokenSource();

            var successResponse =
                _representationModule.GetMessageAsync<FinalCommandExecution>(request.Id,
                    MessageType.FinishedCommandExecution,
                    localTokenSource.Token);
            var errorResponse =
                _representationModule.GetMessageAsync<ExceptionCommandExecution>(requestId: request.Id,
                    MessageType.ExceptionCommandExecution, localTokenSource.Token);

            cancellationToken.Register(async () =>
            {
#if TRACE
                Console.WriteLine("Cancelling task");
#endif
                await _representationModule.PostCallMessageAsync(request.Id, MessageType.CancelRequest,
                    new CancelRequest
                    {
                        Id = request.Id
                    });
                localTokenSource.Cancel();
            });

            Task.WaitAny(new Task[]
            {
                errorResponse, successResponse
            }, cancellationToken);

#if TRACE
            Console.WriteLine($"Handling message");
#endif
            if (successResponse.IsCompletedSuccessfully)
                return;

            if (errorResponse.IsCompletedSuccessfully)
                throw errorResponse.Result.GetException();
        }

        public override string ToString()
        {
            return _identifier.ToString();
        }
    }
}
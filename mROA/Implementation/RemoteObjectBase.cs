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
        public bool Equals(RemoteObjectBase other)
        {
            return _identifier.Equals(other._identifier);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((RemoteObjectBase)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_identifier.GetHashCode(), _identifier.ContextId);
        }

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

            await _representationModule.PostCallMessageAsync(request.Id, EMessageType.CallRequest, request);

            var localTokenSource = new CancellationTokenSource();

            var responseRequestTask = _representationModule.GetSingle(
                m => m.MessageType is EMessageType.FinishedCommandExecution or EMessageType.ExceptionCommandExecution,
                localTokenSource.Token,
                m => m.MessageType is EMessageType.FinishedCommandExecution ? typeof(FinalCommandExecution<T>) : null,
                m => m.MessageType is EMessageType.ExceptionCommandExecution
                    ? typeof(ExceptionCommandExecution)
                    : null);

            cancellationToken.Register(() =>
            {
#if TRACE
                Console.WriteLine("Cancelling task");
#endif
                _representationModule.PostCallMessageAsync(request.Id, EMessageType.CancelRequest,
                    new CancelRequest
                    {
                        Id = request.Id
                    }).ContinueWith(_ => localTokenSource.Cancel());
            });

            var response = await responseRequestTask;

            if (response.Deserialized is FinalCommandExecution<T> successResponse)
            {
                localTokenSource.Cancel();
                return successResponse.Result!;
            }

            localTokenSource.Cancel();
            throw (response.Deserialized as ExceptionCommandExecution)!.GetException();
        }

        protected async Task CallAsync(int methodId, object?[]? parameters = null,
            CancellationToken cancellationToken = default)
        {
            var request = new DefaultCallRequest
            {
                CommandId = methodId, ObjectId = _identifier, Parameters = parameters
            };
            await _representationModule.PostCallMessageAsync(request.Id, EMessageType.CallRequest, request);

            var localTokenSource = new CancellationTokenSource();

            var responseRequestTask = _representationModule.GetSingle(
                m => m.MessageType is EMessageType.FinishedCommandExecution or EMessageType.ExceptionCommandExecution,
                localTokenSource.Token,
                m => m.MessageType is EMessageType.FinishedCommandExecution ? typeof(FinalCommandExecution) : null,
                m => m.MessageType is EMessageType.ExceptionCommandExecution
                    ? typeof(ExceptionCommandExecution)
                    : null);


            cancellationToken.Register(() =>
            {
#if TRACE
                Console.WriteLine("Cancelling task");
#endif
                _representationModule.PostCallMessageAsync(request.Id, EMessageType.CancelRequest,
                    new CancelRequest
                    {
                        Id = request.Id
                    }).ContinueWith(_ => localTokenSource.Cancel());
            });

            var responseRequest = await responseRequestTask;
#if TRACE
            Console.WriteLine($"Handling message");
#endif
            switch (responseRequest.MessageType)
            {
                case EMessageType.FinishedCommandExecution:
                    return;
                case EMessageType.ExceptionCommandExecution:
                    throw (responseRequest.Deserialized as ExceptionCommandExecution)!.GetException();
            }
        }

        protected async Task CallUntrustedAsync(int methodId, object?[]? parameters = null)
        {
            var request = new DefaultCallRequest
            {
                CommandId = methodId, ObjectId = _identifier, Parameters = parameters
            };
            await _representationModule.PostCallMessageUntrustedAsync(request.Id, EMessageType.CallRequest, request);
        }

        public override string ToString()
        {
            return _identifier.ToString();
        }
    }
}
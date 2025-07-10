using System;
using System.Threading;
using System.Threading.Tasks;
using mROA.Abstract;

// ReSharper disable MethodHasAsyncOverload

namespace mROA.Implementation.Frontend
{
    public class RequestExtractor : IRequestExtractor
    {
        private IExecuteModule? _executeModule;

        private IMethodRepository? _methodRepository;

        private IRepresentationModule? _representationModule;
        private IContextualSerializationToolKit? _serializationToolkit;
        private IEndPointContext _context;

        public void Inject(object dependency)
        {
            switch (dependency)
            {
                case IExecuteModule executeModule:
                    _executeModule = executeModule;
                    break;
                case IMethodRepository methodRepository:
                    _methodRepository = methodRepository;
                    break;
                case IRepresentationModule representationModule:
                    _representationModule = representationModule;
                    break;
                case IContextualSerializationToolKit serializationToolkit:
                    _serializationToolkit = serializationToolkit;
                    break;
                case IEndPointContext remoteContext:
                    _context = remoteContext;
                    break;
            }
        }

        public async Task StartExtraction()
        {
            ThrowIfNotInjected();

            var streamTokenSource = new CancellationTokenSource();

            var query = _representationModule!.GetStream(m =>
                    m.MessageType is EMessageType.CallRequest or EMessageType.CancelRequest
                        or EMessageType.EventRequest or EMessageType.ClientDisconnect, _context,
                streamTokenSource.Token,
                m => m.MessageType == EMessageType.CallRequest ? typeof(DefaultCallRequest) : null,
                m => m.MessageType == EMessageType.CancelRequest ? typeof(CancelRequest) : null,
                m => m.MessageType == EMessageType.EventRequest ? typeof(DefaultCallRequest) : null,
                m => m.MessageType == EMessageType.ClientDisconnect ? typeof(ClientDisconnect) : null);


            await foreach (var command in query)
            {
                switch (command.originalType)
                {
                    case EMessageType.CallRequest:
                        HandleCallRequest((DefaultCallRequest)command.parced);
                        break;
                    case EMessageType.ClientDisconnect:
                        return;
                    case EMessageType.EventRequest:
                        HandleEventRequest((DefaultCallRequest)command.parced);
                        break;
                    case EMessageType.CancelRequest:
                        HandleCancelRequest((command.parced as CancelRequest)!);
                        break;
                    default:
                        continue;
                }
            }
        }

        private void ThrowIfNotInjected()
        {
            if (_serializationToolkit == null)
                throw new NullReferenceException("Serializing toolkit is null.");
            if (_executeModule == null)
                throw new NullReferenceException("Execute module is null.");
            if (_representationModule == null)
                throw new NullReferenceException("Representation module is null.");
            if (_methodRepository == null)
                throw new NullReferenceException("Method repository is null.");
        }

        private void HandleCancelRequest(CancelRequest req)
        {
            _executeModule!.Execute(req, _context.RealRepository, _representationModule!, _context);
        }

        private void HandleCallRequest(DefaultCallRequest request)
        {
            var result = _executeModule!.Execute(request, _context.RealRepository, _representationModule!, _context);

            var resultType = result.MessageType;

            if (resultType == EMessageType.Unknown)
            {
                return;
            }

            _representationModule!.PostCallMessage(request.Id, resultType, result, _context);
        }

        private void HandleEventRequest(DefaultCallRequest request)
        {
            _executeModule!.Execute(request, _context.RemoteRepository, _representationModule!, _context);
        }
    }
}
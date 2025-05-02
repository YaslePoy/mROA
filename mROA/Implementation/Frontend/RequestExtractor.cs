using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using mROA.Abstract;
using mROA.Implementation.Backend;

// ReSharper disable MethodHasAsyncOverload

namespace mROA.Implementation.Frontend
{
    public class RequestExtractor : IRequestExtractor
    {
        private IExecuteModule? _executeModule;
        private IMethodRepository? _methodRepository;
        private IContextRepository? _realContextRepository;
        private IContextRepository? _remoteContextRepository;
        private IRepresentationModule? _representationModule;
        private ISerializationToolkit? _serializationToolkit;

        public void Inject<T>(T dependency)
        {
            switch (dependency)
            {
                case IExecuteModule executeModule:
                    _executeModule = executeModule;
                    break;
                case MultiClientContextRepository:
                case ContextRepository:
                    _realContextRepository = dependency as IContextRepository;
                    break;
                case RemoteContextRepository remoteContextRepository:
                    _remoteContextRepository = remoteContextRepository;
                    break;
                case IMethodRepository methodRepository:
                    _methodRepository = methodRepository;
                    break;
                case IRepresentationModule representationModule:
                    _representationModule = representationModule;
                    break;
                case ISerializationToolkit serializationToolkit:
                    _serializationToolkit = serializationToolkit;
                    break;
            }
        }

        public async Task StartExtraction()
        {
            ThrowIfNotInjected();
            var multiClientOwnershipRepository =
                TransmissionConfig.OwnershipRepository as MultiClientOwnershipRepository;
            multiClientOwnershipRepository?.RegisterOwnership(_representationModule!.Id);

            try
            {
#if TRACE
                    var sw = new Stopwatch();
#endif

                var streamTokenSource = new CancellationTokenSource();
                
                var query = _representationModule!.GetStream(m =>
                        m.MessageType is EMessageType.CallRequest or EMessageType.CancelRequest
                            or EMessageType.EventRequest or EMessageType.ClientDisconnect, streamTokenSource.Token,
                    m => m.MessageType == EMessageType.CallRequest ? typeof(DefaultCallRequest) : null,
                    m => m.MessageType == EMessageType.CancelRequest ? typeof(CancelRequest) : null,
                    m => m.MessageType == EMessageType.EventRequest ? typeof(DefaultCallRequest) : null,
                    m => m.MessageType == EMessageType.ClientDisconnect ? typeof(ClientDisconnect) : null);


                await foreach (var command in query)
                {
#if TRACE
                        Console.WriteLine("Waiting for request...");
                        if (sw.IsRunning)
                        {
                            sw.Stop();
                            Console.WriteLine($"Request handling took {Math.Round(sw.Elapsed.TotalMilliseconds * 1000.0)} microseconds.");
                        }
#endif

#if TRACE
                        Console.WriteLine("Request received");
                        sw.Restart();
#endif
                    switch (command.originalType)
                    {
                        case EMessageType.CallRequest:
                            HandleCallRequest((command.parced as DefaultCallRequest)!);
                            break;
                        case EMessageType.ClientDisconnect:
                            return;
                        case EMessageType.EventRequest:
                            HandleEventRequest((command.parced as DefaultCallRequest)!);
                            break;
                        case EMessageType.CancelRequest:
                            HandleCancelRequest((command.parced as CancelRequest)!);

                            break;
                        default:
                            continue;
                    }
                }
            }
            catch
            {
                multiClientOwnershipRepository?.FreeOwnership();
            }
        }

        private void ThrowIfNotInjected()
        {
            if (_serializationToolkit == null)
                throw new NullReferenceException("Serializing toolkit is null.");
            if (_executeModule == null)
                throw new NullReferenceException("Execute module is null.");
            if (_realContextRepository == null)
                throw new NullReferenceException("Context repository is null.");
            if (_representationModule == null)
                throw new NullReferenceException("Representation module is null.");
            if (_methodRepository == null)
                throw new NullReferenceException("Method repository is null.");
        }

        private void HandleCancelRequest(CancelRequest req)
        {
            _executeModule!.Execute(req, _realContextRepository!, _representationModule!);
        }

        private void HandleCallRequest(DefaultCallRequest request)
        {
            var result = _executeModule!.Execute(request, _realContextRepository!, _representationModule!);

            var resultType = result.MessageType;

            if (resultType == EMessageType.Unknown)
            {
                return;
            }

            _representationModule!.PostCallMessage(request.Id, resultType, result, result.GetType());
        }

        private void HandleEventRequest(DefaultCallRequest request)
        {
            _executeModule!.Execute(request, _remoteContextRepository!, _representationModule!);
        }
    }
}
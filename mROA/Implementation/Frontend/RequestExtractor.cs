using System;
using System.Threading;
using System.Threading.Tasks;
using mROA.Abstract;
using mROA.Implementation.Backend;
using mROA.Implementation.CommandExecution;

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

        public Task StartExtraction()
        {
            return Task.Run(() =>
            {
                ThrowIfNotInjected();
                var multiClientOwnershipRepository =
                    TransmissionConfig.OwnershipRepository as MultiClientOwnershipRepository;
                multiClientOwnershipRepository?.RegisterOwnership(_representationModule.Id);

                try
                {
#if TRACE
                    var sw = new Stopwatch();
#endif
                    while (true)
                    {
#if TRACE
                        Console.WriteLine("Waiting for request...");
                        if (sw.IsRunning)
                        {
                            sw.Stop();
                            Console.WriteLine($"Request handling took {Math.Round(sw.Elapsed.TotalMilliseconds * 1000.0)} microseconds.");
                        }
#endif
                        var tokenSource = new CancellationTokenSource();
                        var token = tokenSource.Token;
                        var defaultRequest =
                            _representationModule!.GetMessageAsync<DefaultCallRequest>(
                                messageType: MessageType.CallRequest, token: token);
                        var cancelRequest =
                            _representationModule!.GetMessageAsync<CancelRequest>(
                                messageType: MessageType.CancelRequest, token: token);
                        var eventRequest =
                            _representationModule!.GetMessageAsync<DefaultCallRequest>(
                                messageType: MessageType.EventRequest, token: token);
                        Task.WaitAny(defaultRequest, cancelRequest, eventRequest);
#if TRACE
                        Console.WriteLine("Request received");
                        sw.Restart();
#endif
                        if (cancelRequest.IsCompleted)
                        {
#if TRACE
                            Console.WriteLine("Cancelling request");
#endif
                            HandleCancelRequest(tokenSource, cancelRequest.Result);
                        }
                        else if (defaultRequest.IsCompleted)
                        {
                            HandleCallRequest(tokenSource, defaultRequest.Result);
                        }
                        else
                        {
                            HandleEventRequest(tokenSource, eventRequest.Result);
                        }
                    }
                }
                catch
                {
                    multiClientOwnershipRepository?.FreeOwnership();
                }
            });
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

        private void HandleCancelRequest(CancellationTokenSource tokenSource, CancelRequest req)
        {
            tokenSource.Cancel();
            _executeModule!.Execute(req, _realContextRepository!, _representationModule!);
        }

        private void HandleCallRequest(CancellationTokenSource tokenSource, DefaultCallRequest request)
        {
            tokenSource.Cancel();

            var result = _executeModule!.Execute(request, _realContextRepository!, _representationModule!);

            var resultType = result switch
            {
                FinalCommandExecution => MessageType.FinishedCommandExecution,
                AsyncCommandExecution => MessageType.AsyncCommandExecution,
                ExceptionCommandExecution => MessageType.ExceptionCommandExecution,
                _ => MessageType.Unknown
            };

            _representationModule!.PostCallMessage(request.Id, resultType, result, result.GetType());
        }

        private void HandleEventRequest(CancellationTokenSource tokenSource, DefaultCallRequest request)
        {
            tokenSource.Cancel();
            _executeModule!.Execute(request, _remoteContextRepository!, _representationModule!);
        }
    }
}
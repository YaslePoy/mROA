using System;
using System.Linq;
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
        private IRepresentationModule? _representationModule;
        private IContextRepository? _contextRepository;
        private IMethodRepository? _methodRepository;
        private IExecuteModule? _executeModule;
        private ISerializationToolkit? _serializationToolkit;

        public void Inject<T>(T dependency)
        {
            switch (dependency)
            {
                case IExecuteModule executeModule:
                    _executeModule = executeModule;
                    break;
                case IContextRepository contextRepository:
                    _contextRepository = contextRepository;
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
            if (_serializationToolkit == null)
                throw new NullReferenceException("Serializing toolkit is null.");
            if (_executeModule == null)
                throw new NullReferenceException("Execute module is null.");
            if (_contextRepository == null)
                throw new NullReferenceException("Context repository is null.");
            if (_representationModule == null)
                throw new NullReferenceException("Representation module is null.");
            if (_methodRepository == null)
                throw new NullReferenceException("Method repository is null.");

            await Task.Yield();

            var multiClientOwnershipRepository =
                TransmissionConfig.OwnershipRepository as MultiClientOwnershipRepository;
            multiClientOwnershipRepository?.RegisterOwnership(_representationModule.Id);

            try
            {
                while (true)
                {
#if TRACE
                    Console.WriteLine("Waiting for request...");
#endif
                    var tokenSource = new CancellationTokenSource();
                    var token = tokenSource.Token;
                    var defaultRequest =
                        _representationModule!.GetMessageAsync<DefaultCallRequest>(
                            messageType: MessageType.CallRequest, token: token);
                    var cancelRequest =
                        _representationModule!.GetMessageAsync<CancelRequest>(
                            messageType: MessageType.CancelRequest, token: token);

                    Task.WaitAny(defaultRequest, cancelRequest);
                    
#if TRACE
                    Console.WriteLine("Request received");
#endif
                    if (cancelRequest.IsCompleted)
                    {
#if TRACE
                        Console.WriteLine("Cancelling request");
#endif
                        var req = cancelRequest.Result;
                        tokenSource.Cancel();
                        _executeModule.Execute(req, _contextRepository, _representationModule);
                    }
                    else
                    {
                        tokenSource.Cancel();
                        var request = defaultRequest.Result;

                        if (request.Parameter is not null)
                        {
                            var method = _methodRepository!.GetMethod(request.CommandId);
                            var parameterType = method.GetParameters().First()
                                .ParameterType;

                            request.Parameter = _serializationToolkit.Cast(request.Parameter, parameterType);
                        }

                        var result = _executeModule.Execute(request, _contextRepository, _representationModule);

                        var resultType = MessageType.Unknown;

                        switch (result)
                        {
                            case FinalCommandExecution:
                                resultType = MessageType.FinishedCommandExecution;
                                break;
                            case AsyncCommandExecution:
                                resultType = MessageType.AsyncCommandExecution;
                                break;
                            case ExceptionCommandExecution:
                                resultType = MessageType.ExceptionCommandExecution;
                                break;
                        }

                        _representationModule.PostCallMessage(request.Id, resultType, result, result.GetType());
                    }
                }
            }
            catch
            {
                multiClientOwnershipRepository?.FreeOwnership();
            }
        }
    }
}
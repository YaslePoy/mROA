﻿using System;
using System.Threading;
using mROA.Abstract;
using mROA.Implementation.CommandExecution;

namespace mROA.Implementation.Backend
{
    public class BasicExecutionModule : IExecuteModule
    {
        private ICancellationRepository? _cancellationRepo;
        private IMethodRepository? _methodRepo;
        private ISerializationToolkit? _serialization;

        public void Inject<T>(T dependency)
        {
            switch (dependency)
            {
                case IMethodRepository methodRepo:
                    _methodRepo = methodRepo;
                    break;
                case ICancellationRepository cancellationRepo:
                    _cancellationRepo = cancellationRepo;
                    break;
                case ISerializationToolkit serializationToolkit:
                    _serialization = serializationToolkit;
                    break;
            }
        }

        public ICommandExecution Execute(ICallRequest command, IContextRepository contextRepository,
            IRepresentationModule representationModule)
        {
#if TRACE
            Console.WriteLine(command.GetType().Name);
#endif

            try
            {
                ThrowIfNotInjected(contextRepository);
                if (command is CancelRequest)
                {
#if TRACE
                Console.WriteLine("Final cancelling request");
#endif
                    return CancelExecution(command);
                }

                var invoker = _methodRepo!.GetMethod(command.CommandId);
                if (invoker == null)
                    throw new Exception($"Command {command.CommandId} not found");

                var context = GetContext(command, contextRepository, invoker);

                if (context == null)
                    throw new NullReferenceException("Instance can't be null");


                object?[]? castedParams = null;

                if (invoker.ParameterTypes.Length != 0)
                    castedParams = CastedParams(command, invoker);


                var execContext = new RequestContext(command.Id, representationModule.Id);

                switch (invoker)
                {
                    case AsyncMethodInvoker { IsVoid: false } asyncNonVoidMethodInvoker:
                        return TypedExecuteAsync(asyncNonVoidMethodInvoker, context, castedParams, command,
                            _cancellationRepo!,
                            representationModule, execContext);
                    case AsyncMethodInvoker asyncMethodInvoker:
                        return ExecuteAsync(asyncMethodInvoker, context, castedParams, command, _cancellationRepo!,
                            representationModule, execContext);
                    default:
                        var result = Execute((invoker as MethodInvoker)!, context, castedParams!, command, execContext);
                        if (command.CommandId == -1)
                        {
#if TRACE
                    Console.WriteLine("Disposing object");
#endif
                            contextRepository.ClearObject(command.ObjectId);
                        }

                        return result;
                }
            }
            catch (Exception e)
            {
                return new ExceptionCommandExecution
                {
                    Id = command.Id,
                    Exception = e.ToString()
                };
            }
        }

        private static object GetContext(ICallRequest command, IContextRepository contextRepository, IMethodInvoker invoker)
        {
            var context = command.ObjectId.ContextId != -1
                ? contextRepository.GetObject<object>(command.ObjectId)
                : contextRepository.GetSingleObject(invoker.SuitableType, command.ObjectId.OwnerId);
            return context;
        }

        private object?[] CastedParams(ICallRequest command, IMethodInvoker invoker)
        {
            object?[] castedParams = new object[invoker.ParameterTypes.Length];
            for (var i = 0; i < castedParams.Length; i++)
            {
                castedParams[i] = _serialization!.Cast(command.Parameters![i], invoker.ParameterTypes[i]);
            }

            return castedParams;
        }

        private void ThrowIfNotInjected(IContextRepository contextRepository)
        {
            if (_cancellationRepo is null)
                throw new NullReferenceException("Method repository was not defined");

            if (_methodRepo is null)
                throw new NullReferenceException("Method repository was not defined");

            if (contextRepository is null)
                throw new NullReferenceException("Context repository was not defined");
        }

        private FinalCommandExecution CancelExecution(ICallRequest command)
        {
            var cts = _cancellationRepo!.GetCancellation(command.Id);
            if (cts == null)
                throw new NullReferenceException("Can't find cancellation for this request");
            cts.Cancel();
            _cancellationRepo.FreeCancelation(command.Id);

            return new FinalCommandExecution
            {
                Id = command.Id
            };
        }

        private static ICommandExecution Execute(MethodInvoker invoker, object instance, object?[] parameter,
            ICallRequest command, RequestContext executionContext)
        {
            try
            {
                var finalResult = invoker.Invoke(instance, parameter, new object[] { executionContext });

                if (invoker.IsVoid)
                {
                    return new FinalCommandExecution
                    {
                        Id = command.Id
                    };
                }

                return new FinalCommandExecution<object>
                {
                    Result = finalResult,
                    Id = command.Id
                };
            }
            catch (Exception e)
            {
                return new ExceptionCommandExecution
                {
                    Id = command.Id,
                    Exception = e.ToString()
                };
            }
        }

        private ICommandExecution ExecuteAsync(AsyncMethodInvoker invoker, object instance, object?[]? parameters,
            ICallRequest command, ICancellationRepository cancellationRepository,
            IRepresentationModule representationModule, RequestContext executionContext)
        {
            var tokenSource = new CancellationTokenSource();
            cancellationRepository.RegisterCancellation(command.Id, tokenSource);
            var token = tokenSource.Token;
#if TRACE
            token.Register(() => Console.WriteLine($"Cancellation requested check {command.Id}"));
#endif
            try
            {
                invoker.Invoke(instance, parameters, new object[] { executionContext, token }, _ =>
                {
                    if (token.IsCancellationRequested)
                        return;

                    var payload = new FinalCommandExecution
                    {
                        Id = command.Id
                    };
                    _cancellationRepo?.FreeCancelation(command.Id);

                    var multiClientOwnershipRepository =
                        TransmissionConfig.OwnershipRepository as MultiClientOwnershipRepository;

                    multiClientOwnershipRepository?.RegisterOwnership(representationModule.Id);
                    representationModule.PostCallMessage(command.Id, EMessageType.FinishedCommandExecution, payload);
                    multiClientOwnershipRepository?.FreeOwnership();
                });

                return new AsyncCommandExecution
                {
                    Id = command.Id
                };
            }
            catch (Exception e)
            {
                return new ExceptionCommandExecution
                {
                    Id = command.Id,
                    Exception = e.ToString()
                };
            }
        }

        private ICommandExecution TypedExecuteAsync(AsyncMethodInvoker invoker, object instance, object?[]? parameters,
            ICallRequest command, ICancellationRepository cancellationRepository,
            IRepresentationModule representationModule, RequestContext executionContext)
        {
            var tokenSource = new CancellationTokenSource();
            cancellationRepository.RegisterCancellation(command.Id, tokenSource);

            var token = tokenSource.Token;
            try
            {
                invoker.Invoke(instance, parameters, new object[] { executionContext, token },
                    finalResult =>
                    {
                        var payload = new FinalCommandExecution<object>
                        {
                            Id = command.Id,
                            Result = finalResult
                        };
                        _cancellationRepo!.FreeCancelation(command.Id);

                        var multiClientOwnershipRepository =
                            TransmissionConfig.OwnershipRepository as MultiClientOwnershipRepository;
                        multiClientOwnershipRepository?.RegisterOwnership(representationModule.Id);
                        representationModule.PostCallMessage(command.Id, EMessageType.FinishedCommandExecution,
                            payload);
                        multiClientOwnershipRepository?.FreeOwnership();
                    });

                return new AsyncCommandExecution
                {
                    Id = command.Id
                };
            }
            catch (Exception e)
            {
                return new ExceptionCommandExecution
                {
                    Id = command.Id,
                    Exception = e.ToString()
                };
            }
        }
    }
}
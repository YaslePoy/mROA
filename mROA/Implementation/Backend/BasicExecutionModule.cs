using System;
using System.Threading;
using mROA.Abstract;
using mROA.Implementation.CommandExecution;

namespace mROA.Implementation.Backend
{
    public class BasicExecutionModule : IExecuteModule
    {
        private ICancellationRepository? _cancellationRepo;
        private IMethodRepository? _methodRepo;
        private IContextualSerializationToolKit? _serialization;

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
                case IContextualSerializationToolKit serializationToolkit:
                    _serialization = serializationToolkit;
                    break;
            }
        }

        public ICommandExecution Execute(ICallRequest command, IInstanceRepository instanceRepository,
            IRepresentationModule representationModule, IEndPointContext endPointContext)
        {

            try
            {
                ThrowIfNotInjected(instanceRepository);
                if (command is CancelRequest)
                {
                    return CancelExecution(command);
                }

                var invoker = _methodRepo!.GetMethod(command.CommandId);
                if (invoker == null)
                    throw new Exception($"Command {command.CommandId} not found");

                var context = GetContext(command, instanceRepository, invoker, endPointContext);

                if (context == null)
                    throw new NullReferenceException("Instance can't be null");


                object?[]? castedParams = null;

                if (invoker.ParameterTypes.Length != 0)
                    castedParams = CastedParams(command, invoker, endPointContext);


                var execContext = new RequestContext(command.Id, representationModule.Id);

                switch (invoker)
                {
                    case AsyncMethodInvoker { IsVoid: false } asyncNonVoidMethodInvoker:
                        return TypedExecuteAsync(asyncNonVoidMethodInvoker, context, castedParams, command,
                            _cancellationRepo!,
                            representationModule, execContext, endPointContext);
                    case AsyncMethodInvoker asyncMethodInvoker:
                        return ExecuteAsync(asyncMethodInvoker, context, castedParams, command, _cancellationRepo!,
                            representationModule, execContext, endPointContext);
                    default:
                        var result = Execute((invoker as MethodInvoker)!, context, castedParams!, command, execContext);
                        if (command.CommandId == -1)
                        {
                            instanceRepository.ClearObject(command.ObjectId, endPointContext);
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

        private static object GetContext(ICallRequest command, IInstanceRepository instanceRepository,
            IMethodInvoker invoker, IEndPointContext endPointContext)
        {
            var context = command.ObjectId.ContextId != -1
                ? instanceRepository.GetObject<object>(command.ObjectId, endPointContext)
                : instanceRepository.GetSingleObject(invoker.SuitableType, endPointContext);
            return context;
        }

        private object?[] CastedParams(ICallRequest command, IMethodInvoker invoker, IEndPointContext context)
        {
            object?[] castedParams = new object[invoker.ParameterTypes.Length];
            for (var i = 0; i < castedParams.Length; i++)
            {
                castedParams[i] = _serialization!.Cast(command.Parameters![i], invoker.ParameterTypes[i], context);
            }

            return castedParams;
        }

        private void ThrowIfNotInjected(IInstanceRepository instanceRepository)
        {
            if (_cancellationRepo is null)
                throw new NullReferenceException("Method repository was not defined");

            if (_methodRepo is null)
                throw new NullReferenceException("Method repository was not defined");

            if (instanceRepository is null)
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

                if (!invoker.IsTrusted)
                {
                    return new AsyncCommandExecution();
                }

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
                if (invoker.IsTrusted)
                    return new ExceptionCommandExecution
                    {
                        Id = command.Id,
                        Exception = e.ToString()
                    };
                return new AsyncCommandExecution
                {
                    Id = command.Id
                };
            }
        }

        private ICommandExecution ExecuteAsync(AsyncMethodInvoker invoker, object instance, object?[]? parameters,
            ICallRequest command, ICancellationRepository cancellationRepository,
            IRepresentationModule representationModule, RequestContext executionContext, IEndPointContext context)
        {
            var tokenSource = new CancellationTokenSource();
            cancellationRepository.RegisterCancellation(command.Id, tokenSource);
            var token = tokenSource.Token;

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

            
                    if (invoker.IsTrusted)
                        representationModule.PostCallMessage(command.Id, EMessageType.FinishedCommandExecution,
                            payload, context);
                });

                return new AsyncCommandExecution
                {
                    Id = command.Id
                };
            }
            catch (Exception e)
            {
                if (invoker.IsTrusted)
                    return new ExceptionCommandExecution
                    {
                        Id = command.Id,
                        Exception = e.ToString()
                    };
                return new AsyncCommandExecution
                {
                    Id = command.Id
                };
            }
        }

        private ICommandExecution TypedExecuteAsync(AsyncMethodInvoker invoker, object instance, object?[]? parameters,
            ICallRequest command, ICancellationRepository cancellationRepository,
            IRepresentationModule representationModule, RequestContext executionContext, IEndPointContext context)
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
                        
                        representationModule.PostCallMessage(command.Id, EMessageType.FinishedCommandExecution,
                            payload, context);
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
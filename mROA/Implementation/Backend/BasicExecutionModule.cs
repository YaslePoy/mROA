using System;
using System.Threading;
using mROA.Abstract;
using mROA.Implementation.CommandExecution;

namespace mROA.Implementation.Backend
{
    public class BasicExecutionModule : IExecuteModule
    {
        private readonly ICancellationRepository _cancellationRepo;
        private readonly IMethodRepository _methodRepo;
        private readonly IContextualSerializationToolKit _serialization;

        public BasicExecutionModule(ICancellationRepository cancellationRepo, IMethodRepository methodRepo,
            IContextualSerializationToolKit serialization)
        {
            _cancellationRepo = cancellationRepo;
            _methodRepo = methodRepo;
            _serialization = serialization;
        }

        public ICommandExecution? Execute(ICallRequest command, IInstanceRepository instanceRepository,
            IRepresentationModule representationModule, IEndPointContext endPointContext)
        {
            // _logger.LogInformation("Executing {0}", command.Id);
            try
            {
                if (command is CancelRequest)
                {
                    return CancelExecution(command);
                }

                var invoker = _methodRepo.GetMethod(command.CommandId);
                if (invoker == null)
                    throw new Exception($"Command {command.CommandId} not found");

                var instance = GetInstance(command, instanceRepository, invoker, endPointContext);

                if (instance is null)
                    throw new NullReferenceException("Instance can't be null");


                object?[]? castedParams = null;

                if (invoker.ParameterTypes.Length != 0)
                    castedParams = CastedParams(command, invoker, endPointContext);


                var execContext = new RequestContext(command.Id, representationModule.Id);

                var executionResult = ExecuteRequest(command, instanceRepository, representationModule, endPointContext,
                    invoker,
                    instance, castedParams, execContext);
                return executionResult;
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

        private ICommandExecution? ExecuteRequest(ICallRequest command, IInstanceRepository instanceRepository,
            IRepresentationModule representationModule, IEndPointContext endPointContext, IMethodInvoker invoker,
            object context, object?[]? castedParams, RequestContext execContext)
        {
            switch (invoker)
            {
                case AsyncMethodInvoker { IsVoid: false } asyncNonVoidMethodInvoker:
                    return TypedExecuteAsync(asyncNonVoidMethodInvoker, context, castedParams, command,
                        _cancellationRepo,
                        representationModule, execContext, endPointContext);
                case AsyncMethodInvoker asyncMethodInvoker:
                    return ExecuteAsync(asyncMethodInvoker, context, castedParams, command, _cancellationRepo,
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

        private static object GetInstance(ICallRequest command, IInstanceRepository instanceRepository,
            IMethodInvoker invoker, IEndPointContext endPointContext)
        {
            var context = command.ObjectId.ContextId != -1
                ? instanceRepository.GetObject<object>(command.ObjectId, endPointContext)
                : instanceRepository.GetSingletonObject(invoker.SuitableType, endPointContext);

            return context;
        }

        private object?[] CastedParams(ICallRequest command, IMethodInvoker invoker, IEndPointContext context)
        {
            object?[] castedParams = new object[invoker.ParameterTypes.Length];
            for (var i = 0; i < castedParams.Length; i++)
            {
                castedParams[i] = _serialization.Cast(command.Parameters![i], invoker.ParameterTypes[i], context);
            }

            return castedParams;
        }

        private FinalCommandExecution CancelExecution(ICallRequest command)
        {
            var cts = _cancellationRepo.GetCancellation(command.Id);
            if (cts == null)
                throw new NullReferenceException("Can't find cancellation for this request");
            cts.Cancel();
            _cancellationRepo.FreeCancellation(command.Id);

            return new FinalCommandExecution
            {
                Id = command.Id
            };
        }

        private static ICommandExecution? Execute(MethodInvoker invoker, object instance, object?[] parameter,
            ICallRequest command, RequestContext executionContext)
        {
            var finalResult = invoker.Invoke(instance, parameter, new object[] { executionContext });

            if (!invoker.IsTrusted)
            {
                return null;
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

        private ICommandExecution? ExecuteAsync(AsyncMethodInvoker invoker, object instance, object?[]? parameters,
            ICallRequest command, ICancellationRepository cancellationRepository,
            IRepresentationModule representationModule, RequestContext executionContext, IEndPointContext context)
        {
            var tokenSource = new CancellationTokenSource();
            cancellationRepository.RegisterCancellation(command.Id, tokenSource);
            var token = tokenSource.Token;

            invoker.Invoke(instance, parameters, new object[] { executionContext, token }, _ =>
            {
                if (token.IsCancellationRequested)
                    return;

                var payload = new FinalCommandExecution
                {
                    Id = command.Id
                };
                _cancellationRepo.FreeCancellation(command.Id);


                if (invoker.IsTrusted)
                    representationModule.PostCallMessageAsync(command.Id, EMessageType.FinishedCommandExecution,
                        payload, context);
            });


            return null;
        }

        private ICommandExecution? TypedExecuteAsync(AsyncMethodInvoker invoker, object instance, object?[]? parameters,
            ICallRequest command, ICancellationRepository cancellationRepository,
            IRepresentationModule representationModule, RequestContext executionContext, IEndPointContext context)
        {
            var tokenSource = new CancellationTokenSource();
            cancellationRepository.RegisterCancellation(command.Id, tokenSource);

            var token = tokenSource.Token;

            invoker.Invoke(instance, parameters, new object[] { executionContext, token },
                finalResult =>
                {
                    var payload = new FinalCommandExecution<object>
                    {
                        Id = command.Id,
                        Result = finalResult
                    };
                    _cancellationRepo.FreeCancellation(command.Id);

                    representationModule.PostCallMessageAsync(command.Id, EMessageType.FinishedCommandExecution,
                        payload, context);
                });

            return null;
        }
    }
}
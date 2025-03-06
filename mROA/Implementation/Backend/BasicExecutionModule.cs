using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using mROA.Abstract;
using mROA.Implementation.CommandExecution;

namespace mROA.Implementation.Backend
{
    public class BasicExecutionModule : IExecuteModule
    {
        private IMethodRepository? _methodRepo;
        private ICancellationRepository? _cancellationRepo;
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
                if (_cancellationRepo is null)
                    throw new NullReferenceException("Method repository was not defined");

                if (_methodRepo is null)
                    throw new NullReferenceException("Method repository was not defined");

                if (contextRepository is null)
                    throw new NullReferenceException("Context repository was not defined");

                if (command is CancelRequest)
                {
#if TRACE
                Console.WriteLine("Final cancelling request");
#endif
                    var cts = _cancellationRepo.GetCancellation(command.Id);
                    if (cts == null)
                        throw new NullReferenceException("Can't find cancellation for this request");
                    cts.Cancel();
                    _cancellationRepo.FreeCancelation(command.Id);
                    
                    return new FinalCommandExecution
                    {
                        Id = command.Id
                    };
                }

                var invoker = _methodRepo.GetMethod(command.CommandId);
                if (invoker == null)
                    throw new Exception($"Command {command.CommandId} not found");

                var context = command.ObjectId != -1
                    ? contextRepository.GetObject<object>(command.ObjectId)
                    : contextRepository.GetSingleObject(invoker.SuitableType);

                if (context == null)
                    throw new NullReferenceException("Instance can't be null");
                

                object?[]? castedParams = null;

                if (invoker.ParameterTypes != Type.EmptyTypes)
                {
                    castedParams = new object[invoker.ParameterTypes.Length];
                    for (int i = 0; i < castedParams.Length; i++)
                    {
                        castedParams[i] = _serialization.Cast(command.Parameters![i], invoker.ParameterTypes[i]);
                    }
                }
                
                var execContext = new RequestContext(command.Id, representationModule.Id);

                if (invoker is { IsAsync: true, IsVoid: false })
                    return TypedExecuteAsync(invoker, context, castedParams, command, _cancellationRepo,
                        representationModule, execContext);

                if (invoker.IsAsync)
                    return ExecuteAsync(invoker, context, castedParams, command, _cancellationRepo,
                        representationModule, execContext);

                var result = Execute(invoker, context, castedParams, command, execContext);
                if (command.CommandId == -1)
                {
#if TRACE
                    Console.WriteLine("Disposing object");
#endif
                    contextRepository.ClearObject(command.ObjectId);
                }

                return result;
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

        private static ICommandExecution Execute(IMethodInvoker invoker, object instance, object?[] parameter,
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

        private ICommandExecution ExecuteAsync(IMethodInvoker invoker, object instance, object?[]? parameters,
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
                var result = (Task)invoker.Invoke(instance, parameters, new object[] { executionContext, token })!;


                result.ContinueWith(_ =>
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
                    representationModule.PostCallMessage(command.Id, MessageType.FinishedCommandExecution, payload);
                    multiClientOwnershipRepository?.FreeOwnership();
                }, token);

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

        private ICommandExecution TypedExecuteAsync(IMethodInvoker invoker, object instance, object?[]? parameters,
            ICallRequest command, ICancellationRepository cancellationRepository,
            IRepresentationModule representationModule, RequestContext executionContext)
        {
            var tokenSource = new CancellationTokenSource();
            cancellationRepository.RegisterCancellation(command.Id, tokenSource);

            var token = tokenSource.Token;
            try
            {
                var result =
                    (Task)invoker.Invoke(instance, parameters, new object[] { executionContext, token })!;

                result.ContinueWith(t =>
                {
                    var finalResult = t.GetType().GetProperty("Result")?.GetValue(t);
                    var payload = new FinalCommandExecution<object>
                    {
                        Id = command.Id,
                        Result = finalResult
                    };
                    _cancellationRepo!.FreeCancelation(command.Id);

                    var multiClientOwnershipRepository =
                        TransmissionConfig.OwnershipRepository as MultiClientOwnershipRepository;
                    multiClientOwnershipRepository?.RegisterOwnership(representationModule.Id);
                    representationModule.PostCallMessage(command.Id, MessageType.FinishedCommandExecution, payload);
                    multiClientOwnershipRepository?.FreeOwnership();
                }, token);

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
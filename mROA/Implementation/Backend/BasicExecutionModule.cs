using System;
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

        public void Inject<T>(T dependency)
        {
            if (dependency is IMethodRepository methodRepo) _methodRepo = methodRepo;
            if (dependency is ICancellationRepository cancellationRepo) _cancellationRepo = cancellationRepo;
        }

        public ICommandExecution Execute(ICallRequest command, IContextRepository contextRepository,
            IRepresentationModule representationModule)
        {
            Console.WriteLine(command.GetType().Name);

            if (_cancellationRepo is null)
                throw new NullReferenceException("Method repository was not defined");

            if (_methodRepo is null)
                throw new NullReferenceException("Method repository was not defined");

            if (contextRepository is null)
                throw new NullReferenceException("Context repository was not defined");

            if (command is CancelRequest)
            {
                Console.WriteLine("Final cancelling request");
                var cts = _cancellationRepo.GetCancellation(command.Id);
                cts.Cancel();
                _cancellationRepo.FreeCancelation(command.Id);
                return new FinalCommandExecution
                {
                    Id = command.Id,
                    CommandId = command.CommandId
                };
            }

            var currentCommand = _methodRepo.GetMethod(command.CommandId);
            if (currentCommand == null)
                throw new Exception($"Command {command.CommandId} not found");

            var context = command.ObjectId != -1
                ? contextRepository.GetObject(command.ObjectId)
                : contextRepository.GetSingleObject(currentCommand.DeclaringType!);
            var parameter = command.Parameter;

            if (currentCommand.ReturnType.BaseType == typeof(Task) &&
                currentCommand.ReturnType.GenericTypeArguments.Length == 1)
                return TypedExecuteAsync(currentCommand, context, parameter, command, _cancellationRepo,
                    representationModule);

            if (currentCommand.ReturnType == typeof(Task))
                return ExecuteAsync(currentCommand, context, parameter, command, _cancellationRepo,
                    representationModule);

            try
            {
                var result = Execute(currentCommand, context, parameter, command);
                if (command.CommandId == -1)
                {
                    Console.WriteLine("Disposing object");
                    contextRepository.ClearObject(command.ObjectId);
                }
            
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }


        }

        private static ICommandExecution Execute(MethodInfo currentCommand, object context, object? parameter,
            ICallRequest command)
        {
            try
            {
                var finalResult = currentCommand.Invoke(context, parameter is null
                    ? Array.Empty<object>()
                    : new[]
                        { parameter });
                
                return new TypedFinalCommandExecution
                {
                    CommandId = command.CommandId, Result = finalResult,
                    Id = command.Id,
                    Type = currentCommand.ReturnType
                };
            }
            catch (Exception e)
            {
                return new ExceptionCommandExecution
                {
                    Id = command.Id, CommandId = command.CommandId,
                    Exception = e.ToString()
                };
            }
        }

        private ICommandExecution ExecuteAsync(MethodInfo currentCommand, object context, object? parameter,
            ICallRequest command, ICancellationRepository cancellationRepository,
            IRepresentationModule representationModule)
        {
            var tokenSource = new CancellationTokenSource();
            cancellationRepository.RegisterCancellation(command.Id, tokenSource);
            var token = tokenSource.Token;
            token.Register(() => Console.WriteLine($"Cancellation requested check {command.Id}"));
            try
            {
                var result = (Task)currentCommand.Invoke(context, parameter is null
                    ? new object[] { token }
                    : new[]
                        { parameter, token })!;


                result.ContinueWith(_ =>
                {
                    if (token.IsCancellationRequested)
                        return;
                    
                    var payload = new FinalCommandExecution
                    {
                        Id = command.Id,
                        CommandId = command.CommandId
                    };
                    _cancellationRepo.FreeCancelation(command.Id);

                    var multiClientOwnershipRepository =
                        TransmissionConfig.OwnershipRepository as MultiClientOwnershipRepository;
                    multiClientOwnershipRepository?.RegisterOwnership(representationModule.Id);
                    representationModule.PostCallMessage(command.Id, MessageType.FinishedCommandExecution, payload);
                    multiClientOwnershipRepository?.FreeOwnership();
                }, token);

                return new AsyncCommandExecution
                {
                    Id = command.Id, CommandId = command.CommandId
                };
            }
            catch (Exception e)
            {
                return new ExceptionCommandExecution
                {
                    Id = command.Id, CommandId = command.CommandId,
                    Exception = e.ToString()
                };
            }
        }

        private ICommandExecution TypedExecuteAsync(MethodInfo currentCommand, object context, object? parameter,
            ICallRequest command, ICancellationRepository cancellationRepository,
            IRepresentationModule representationModule)
        {
            var tokenSource = new CancellationTokenSource();
            cancellationRepository.RegisterCancellation(command.Id, tokenSource);

            var token = tokenSource.Token;
            try
            {
                var result =
                    (Task)currentCommand.Invoke(context, parameter is null
                        ? new object[] { token }
                        : new[]
                            { parameter, token })!;

                result.ContinueWith(t =>
                {
                    var finalResult = t.GetType().GetProperty("Result")?.GetValue(t);
                    var payload = new TypedFinalCommandExecution
                    {
                        Id = command.Id,
                        Result = finalResult,
                        CommandId = command.CommandId,
                        Type = finalResult?.GetType()
                    };
                    _cancellationRepo.FreeCancelation(command.Id);

                    var multiClientOwnershipRepository =
                        TransmissionConfig.OwnershipRepository as MultiClientOwnershipRepository;
                    multiClientOwnershipRepository?.RegisterOwnership(representationModule.Id);
                    representationModule.PostCallMessage(command.Id, MessageType.FinishedCommandExecution, payload);
                    multiClientOwnershipRepository?.FreeOwnership();
                }, token);

                return new AsyncCommandExecution
                {
                    Id = command.Id, CommandId = command.CommandId
                };
            }
            catch (Exception e)
            {
                return new ExceptionCommandExecution
                {
                    Id = command.Id, CommandId = command.CommandId,
                    Exception = e.ToString()
                };
            }
        }
    }
}
using System.Reflection;

namespace mROA.Implementation;

public class PrepairedExecutionModule : IExecuteModule
{
    private readonly IMethodRepository _methodRepo;
    private readonly ISerialisationModule _serialisationModule;
    private readonly IContextRepository _contextRepo;
    private readonly MethodInfo _resultExtractionMethod;
    
    public PrepairedExecutionModule(IMethodRepository methodRepo,
        ISerialisationModule serialisationModule,
        IContextRepository contextRepo)
    {
        _methodRepo = methodRepo;
        _serialisationModule = serialisationModule;
        _contextRepo = contextRepo;
        _serialisationModule.SetExecuteModule(this);
        
    }

    public ICommandExecution Execute(ICallRequest command)
    {
        var currentCommand = _methodRepo.GetMethod(command.CommandId);
        if (currentCommand == null)
            throw new Exception($"Command {command.CommandId} not found");

        var context = command is CallRequest request ? _contextRepo.GetObject(request.ObjectId) : _contextRepo.GetSingleObject(currentCommand.DeclaringType);
        var parameter = command is ParametrizedCallRequest callRequest ? callRequest.Parameter : null;

        if (currentCommand.ReturnType.BaseType == typeof(Task) && currentCommand.ReturnType.GenericTypeArguments.Length == 1)
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            var result =
                currentCommand.Invoke(context, parameter is null ? [token] : [parameter, token]) as Task;
            var exec = new AsyncCommandExecution(tokenSource)
                { CommandId = command.CommandId, ClientId = command.ClientId };
            
            result.ContinueWith(task =>
            {
                var result = task.GetType().GetProperty("Result").GetValue(task);
                _serialisationModule.PostResponse(new FinalCommandExecution
                {
                    ExecutionId = exec.ExecutionId, Result = result, CommandId = command.CommandId,
                    ClientId = command.ClientId
                });
            }, token);
            
            // result.ContinueWith(task =>
            // {
            //     var result = task.Result;
            //     _serialisationModule.PostResponse(new FinalCommandExecution
            //     {
            //         ExecutionId = exec.ExecutionId, Result = result, CommandId = command.CommandId,
            //         ClientId = command.ClientId
            //     });
            // }, token);

            return exec;
        }

        if (currentCommand.ReturnType == typeof(Task))
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            var result = currentCommand.Invoke(context, parameter is null ? [token] : [parameter, token]) as Task;
            var exec = new AsyncCommandExecution(tokenSource)
                { CommandId = command.CommandId, ClientId = command.ClientId };

            result.ContinueWith(_ =>
            {
                _serialisationModule.PostResponse(new FinalCommandExecution
                {
                    ExecutionId = exec.ExecutionId, Result = null, CommandId = command.CommandId,
                    ClientId = command.ClientId
                });
            }, token);

            return exec;
        }

        var finalResult = currentCommand.Invoke(context, parameter is null ? [] : [parameter]);
        return new FinalCommandExecution { CommandId = command.CommandId, Result = finalResult };
    }
}
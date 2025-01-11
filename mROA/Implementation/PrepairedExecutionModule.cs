using System.Reflection;

namespace mROA.Implementation;

public class PrepairedExecutionModule(
    IMethodRepository methodRepo,
    IInputModule inputModule,
    IContextRepository contextRepo)
    : IExecuteModule
{
    public ICommandExecution Execute(ICallRequest command)
    {
        var currentCommand = methodRepo.GetMethod(command.CommandId);
        if (currentCommand == null)
            throw new Exception($"Command {command.CommandId} not found");

        var context = command is CallRequest request ? contextRepo.GetObject(request.ObjectId) : null;
        var parameter = command is ParametrizedCallRequest callRequest ? callRequest.Parameter : null;

        if (currentCommand.ReturnType == typeof(Task<>))
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            var result =
                currentCommand.Invoke(context, parameter is null ? [token] : [parameter, token]) as Task<object>;
            var exec = new AsyncCommandExecution(tokenSource)
                { CommandId = command.CommandId, ClientId = command.ClientId };

            result.ContinueWith(task =>
            {
                var result = task.Result;
                inputModule.PostResponse(new FinalCommandExecution
                {
                    ExecutionId = exec.ExecutionId, Result = result, CommandId = command.CommandId,
                    ClientId = command.ClientId
                });
            }, token);

            return exec;
        }
        else
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            var result = currentCommand.Invoke(context, parameter is null ? [token] : [parameter, token]) as Task;
            var exec = new AsyncCommandExecution(tokenSource)
                { CommandId = command.CommandId, ClientId = command.ClientId };

            result.ContinueWith(_ =>
            {
                inputModule.PostResponse(new FinalCommandExecution
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
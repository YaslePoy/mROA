using System.Reflection;

namespace mROA.Implementation;

public class PrepairedExecutionModule(
    IMethodRepository methodRepo,
    IInputModule inputModule,
    IContextRepository contextRepo)
    : IExecuteModule
{
    public ICommandExecution Execute(int objectId, int commandId, object parameter)
    {
        var command = methodRepo.GetMethod(commandId);
        if (command == null)
            throw new Exception($"Command {commandId} not found");

        if (command.ReturnType != typeof(Task))
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            var result = command.Invoke(contextRepo.GetObject(objectId), [parameter, token]) as Task<object>;

            var exec = new AsyncCommandExecution(tokenSource) { CommandId = commandId };

            result.ContinueWith(task =>
            {
                var result = task.Result;
                inputModule.PostResponse(new FinalCommandExecution
                    { ExecutionId = exec.ExecutionId, Result = result, CommandId = commandId });
            }, token);

            return exec;
        }

        var finalResult = command.Invoke(contextRepo.GetObject(objectId), [parameter]);
        return new FinalCommandExecution { CommandId = commandId, Result = finalResult };
    }
}
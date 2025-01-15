using System.Reflection;

namespace mROA.Implementation;

public class LaunchReadyExecutionModule : IExecuteModule
{
    private readonly IMethodRepository _methodRepo;
    private readonly ISerialisationModule _serialisationModule;
    private readonly IContextRepository _contextRepo;

    public LaunchReadyExecutionModule(IMethodRepository methodRepo,
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

        var context = command.ObjectId != -1
            ? _contextRepo.GetObject(command.ObjectId)
            : _contextRepo.GetSingleObject(currentCommand.DeclaringType!);
        var parameter = command.Parameter;

        if (currentCommand.ReturnType.BaseType == typeof(Task) &&
            currentCommand.ReturnType.GenericTypeArguments.Length == 1)
            return TypedExecuteAsync(currentCommand, context, parameter, command);

        if (currentCommand.ReturnType == typeof(Task))
            return ExecuteAsync(currentCommand, context, parameter, command);

        return Execute(currentCommand, context, parameter, command);
    }

    private static FinalCommandExecution Execute(MethodInfo currentCommand, object context, object? parameter,
        ICallRequest command)
    {
        var finalResult = currentCommand.Invoke(context, parameter is null ? [] : [parameter]);
        return new FinalCommandExecution
        {
            CommandId = command.CommandId, Result = finalResult, ClientId = command.ClientId,
            CallRequestId = command.CallRequestId
        };
    }

    private AsyncCommandExecution ExecuteAsync(MethodInfo currentCommand, object context, object? parameter,
        ICallRequest command)
    {
        var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        var result = (Task)currentCommand.Invoke(context, parameter is null ? [token] : [parameter, token])!;
        var exec = new AsyncCommandExecution(tokenSource)
            { CommandId = command.CommandId, ClientId = command.ClientId, CallRequestId = command.CallRequestId };

        result.ContinueWith(_ => { PostFinalizedCallback(exec, null); }, token);

        return exec;
    }

    private AsyncCommandExecution TypedExecuteAsync(MethodInfo currentCommand, object context, object? parameter,
        ICallRequest command)
    {
        var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        var result =
            (Task)currentCommand.Invoke(context, parameter is null ? [token] : [parameter, token])!;
        var exec = new AsyncCommandExecution(tokenSource)
            { CommandId = command.CommandId, ClientId = command.ClientId, CallRequestId = command.CallRequestId };

        result.ContinueWith(task =>
        {
            var finalResult = task.GetType().GetProperty("Result")?.GetValue(task);
            PostFinalizedCallback(exec, finalResult);
        }, token);

        return exec;
    }

    private void PostFinalizedCallback(AsyncCommandExecution request, object? result)
    {
        _serialisationModule.PostResponse(new FinalCommandExecution
        {
            CallRequestId = request.CallRequestId,
            Result = result,
            CommandId = request.CommandId,
            ClientId = request.ClientId
        });
    }
}
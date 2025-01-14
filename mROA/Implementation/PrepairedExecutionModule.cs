﻿using System.Reflection;

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

        var context = command.ObjectId != -1
            ? _contextRepo.GetObject(command.ObjectId)
            : _contextRepo.GetSingleObject(currentCommand.DeclaringType);
        var parameter = command.Parameter;

        if (currentCommand.ReturnType.BaseType == typeof(Task) &&
            currentCommand.ReturnType.GenericTypeArguments.Length == 1)
            return TypedExecuteAsync(currentCommand, context, parameter, command);
        
        if (currentCommand.ReturnType == typeof(Task))
            return ExecuteAsync(currentCommand, context, parameter, command);
        
        return Execute(currentCommand, context, parameter, command);
    }

    private ICommandExecution Execute(MethodInfo currentCommand, object context, object parameter, ICallRequest request)
    {
        var finalResult = currentCommand.Invoke(context, parameter is null ? [] : [parameter]);
        return new FinalCommandExecution
            { CommandId = request.CommandId, Result = finalResult, ClientId = request.ClientId };
    }

    private ICommandExecution ExecuteAsync(MethodInfo currentCommand, object context, object parameter,
        ICallRequest command)
    {
        var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        var result = currentCommand.Invoke(context, parameter is null ? [token] : [parameter, token]) as Task;
        var exec = new AsyncCommandExecution(tokenSource)
            { CommandId = command.CommandId, ClientId = command.ClientId };

        result.ContinueWith(_ =>
        {
            //_serialisationModule.PostResponse(new FinalCommandExecution
            //{
            //    ExecutionId = exec.ExecutionId, Result = null, CommandId = command.CommandId,
            //    ClientId = command.ClientId
            //});
            PostFinalizedCallback(exec, null);

        }, token);

        return exec;
    }

    private ICommandExecution TypedExecuteAsync(MethodInfo currentCommand, object context, object parameter,
        ICallRequest command)
    {
        var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        var result =
            currentCommand.Invoke(context, parameter is null ? [token] : [parameter, token]) as Task;
        var exec = new AsyncCommandExecution(tokenSource)
            { CommandId = command.CommandId, ClientId = command.ClientId };

        result.ContinueWith(task =>
        {
            var finalResult = task.GetType().GetProperty("Result").GetValue(task);
            //_serialisationModule.PostResponse(new FinalCommandExecution
            //{
            //    ExecutionId = exec.ExecutionId, Result = result, CommandId = command.CommandId,
            //    ClientId = command.ClientId
            //});
            PostFinalizedCallback(exec, finalResult);
        }, token);

        return exec;
    }

    private void PostFinalizedCallback(ICommandExecution request, object result)
    {
        _serialisationModule.PostResponse(new FinalCommandExecution
        {
            ExecutionId = request.ExecutionId,
            Result = request,
            CommandId = request.CommandId,
            ClientId = request.ClientId
        });
    }
}
﻿using System.Reflection;
using mROA.Abstract;
using mROA.Implementation.CommandExecution;

namespace mROA.Implementation.Backend;

public class BasicExecutionModule : IExecuteModule
{
    private IMethodRepository? _methodRepo;

    public void Inject<T>(T dependency)
    {
        if (dependency is IMethodRepository methodRepo) _methodRepo = methodRepo;
    }

    public ICommandExecution Execute(ICallRequest command, IContextRepository contextRepository)
    {
        if (_methodRepo is null)
            throw new NullReferenceException("Method repository was not defined");
        
        if (contextRepository is null)
            throw new NullReferenceException("Context repository was not defined");
        
        var currentCommand = _methodRepo.GetMethod(command.CommandId);
        if (currentCommand == null)
            throw new Exception($"Command {command.CommandId} not found");

        var context = command.ObjectId != -1
            ? contextRepository.GetObject(command.ObjectId)
            : contextRepository.GetSingleObject(currentCommand.DeclaringType!);
        
        var parameter = command.Parameter;

        if (currentCommand.ReturnType.BaseType == typeof(Task) &&
            currentCommand.ReturnType.GenericTypeArguments.Length == 1)
            return TypedExecuteAsync(currentCommand, context, parameter, command);

        if (currentCommand.ReturnType == typeof(Task))
            return ExecuteAsync(currentCommand, context, parameter, command);

        return Execute(currentCommand, context, parameter, command);
    }

    private static ICommandExecution Execute(MethodInfo currentCommand, object context, object? parameter,
        ICallRequest command)
    {
        try
        {
            var finalResult = currentCommand.Invoke(context, parameter is null ? [] : [parameter]);
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

    private static ICommandExecution ExecuteAsync(MethodInfo currentCommand, object context, object? parameter,
        ICallRequest command)
    {
        var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;
        try
        {
            var result = (Task)currentCommand.Invoke(context, parameter is null ? [token] : [parameter, token])!;


            result.Wait(token);


            return new FinalCommandExecution { CommandId = command.CommandId, Id = command.Id };
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

    private static ICommandExecution TypedExecuteAsync(MethodInfo currentCommand, object context, object? parameter,
        ICallRequest command)
    {
        var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;
        try
        {
            var result =
                (Task)currentCommand.Invoke(context, parameter is null ? [token] : [parameter, token])!;

            result.Wait(token);

            var finalResult = result.GetType().GetProperty("Result")?.GetValue(result);
            return new TypedFinalCommandExecution
            {
                Id = command.Id,
                Result = finalResult,
                CommandId = command.CommandId,
                Type = finalResult?.GetType()
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
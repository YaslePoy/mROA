namespace mROA.Implementation;

public class FinalCommandExecution : ICommandExecution
{
    public Guid ExecutionId { get; init; } = Guid.NewGuid();
    public int ClientId { get; set; }
    public int CommandId { get; set; }
    public object? Result { get; set; }
}

public class ExeptionCommandExecution : ICommandExecution
{
    public Guid ExecutionId { get; init; } = Guid.NewGuid();
    public int ClientId { get; set; }
    public int CommandId { get; set; }
    public string Reason { get; set; }
}

public class AsyncCommandExecution : ICommandExecution
{
    public Guid ExecutionId { get; init; } = Guid.NewGuid();
    public int ClientId { get; set; }
    public int CommandId { get; set; }

    public void Cancel()
    {
        LocalToken.Cancel();
    }

    private CancellationTokenSource LocalToken;

    public AsyncCommandExecution(CancellationTokenSource localToken)
    {
        LocalToken = localToken;
    }
}
using System.Text.Json.Serialization;

namespace mROA.Implementation;

public class FinalCommandExecution : ICommandExecution
{
    public Guid CallRequestId { get; init; }
    [JsonIgnore]
    public int ClientId { get; set; }
    [JsonIgnore]
    public int CommandId { get; set; }
    public object? Result { get; set; }
}

public class ExeptionCommandExecution : ICommandExecution
{
    public Guid CallRequestId { get; init; }
    public int ClientId { get; set; }
    public int CommandId { get; set; }
    public string Reason { get; set; }
}

public class AsyncCommandExecution : ICommandExecution
{
    public Guid CallRequestId { get; init; }
    [JsonIgnore]
    public int ClientId { get; set; }
    [JsonIgnore]
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
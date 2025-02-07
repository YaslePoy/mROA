using System.Text.Json.Serialization;
using mROA.Abstract;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace mROA.Implementation;

public class FinalCommandExecution : ICommandExecution
{
    public Guid CallRequestId { get; init; }
    [JsonIgnore]
    public int ClientId { get; set; }
    [JsonIgnore]
    public int CommandId { get; init; }
}

public class FinalCommandExecution<T> : FinalCommandExecution
{
    public T? Result { get; init; }
}

public class TypedFinalCommandExecution : FinalCommandExecution<object>
{
    [JsonIgnore]
    public Type? Type { get; set; }
}

public class ExceptionCommandExecution : ICommandExecution
{
    public Guid CallRequestId { get; init; }
    public int ClientId { get; set; }
    public int CommandId { get; init; }
    public required string Exception { get; set; }
}
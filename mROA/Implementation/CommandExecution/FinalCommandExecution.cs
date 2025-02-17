using System.Text.Json.Serialization;
using mROA.Abstract;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace mROA.Implementation;

public class FinalCommandExecution : ICommandExecution
{
    public Guid Id { get; init; }
    [JsonIgnore]
    public int ClientId { get; set; }
    [JsonIgnore]
    public int CommandId { get; init; }
}

public class FinalCommandExecution<T> : FinalCommandExecution
{
    public T? Result { get; init; }
}
using System.Text.Json.Serialization;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace mROA.Implementation;

public interface ICallRequest
{
    Guid Id { get; }
    int CommandId { get; }
    int ObjectId { get; }
    object? Parameter { get; }
}

public class DefaultCallRequest : ICallRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int CommandId { get; init; }
    public int ObjectId { get; init; } = -1;
    
    [JsonIgnore]
    public Type? ParameterType { get; init; }
    public object? Parameter { get; set; }
}
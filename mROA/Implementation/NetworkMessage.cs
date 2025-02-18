using System.Text.Json.Serialization;
// ReSharper disable UnusedMember.Global

namespace mROA.Implementation;

public class NetworkMessage
{
    public Guid Id { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MessageType SchemaId { get; init; }

    public required byte[] Data { get; init; }
}

public enum MessageType
{
    Unknown, FinishedCommandExecution, ExceptionCommandExecution, AsyncCancelCommandExecution, CallRequest, IdAssigning 
}
using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global

namespace mROA.Implementation;

public class NetworkMessage
{
    public static readonly NetworkMessage Null = new() { SchemaId = MessageType.Unknown, Id = Guid.Empty, Data = [] };
    public Guid Id { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MessageType SchemaId { get; init; }

    public required byte[] Data { get; init; }

    public bool IsValidMessage(Guid? requestId = null, MessageType? messageType = null)
    {
        return (requestId is null || Id == requestId) &&
               (messageType is null || SchemaId == messageType);
    }
}

public enum MessageType
{
    Unknown,
    FinishedCommandExecution,
    ExceptionCommandExecution,
    AsyncCancelCommandExecution,
    CallRequest,
    IdAssigning
}
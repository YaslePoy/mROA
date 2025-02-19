using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global

namespace mROA.Implementation;

public sealed class NetworkMessage
{
    public static readonly NetworkMessage Null = new() { MessageType = EMessageType.Unknown, Id = Guid.Empty, Data = [] };
    public Guid Id { get; init; }
    public EMessageType MessageType { get; init; }
    public required byte[] Data { get; init; }
    public bool IsValidMessage(Guid? requestId = null, EMessageType? messageType = null)
    {
        return (requestId is null || Id == requestId) &&
               (messageType is null || MessageType == messageType);
    }
}

public enum EMessageType
{
    Unknown,
    FinishedCommandExecution,
    ExceptionCommandExecution,
    AsyncCancelCommandExecution,
    CallRequest,
    IdAssigning
}
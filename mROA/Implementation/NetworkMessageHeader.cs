using System;
using System.Text.Json.Serialization;
using mROA.Implementation.Attributes;

// ReSharper disable UnusedMember.Global

namespace mROA.Implementation
{
    public interface INetworkMessage
    {
        [SerializationIgnore]
        public EMessageType MessageType { get; }
    }
    public class NetworkMessageHeader
    {
        public Guid Id { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EMessageType EMessageType { get; set; }

        public byte[] Data { get; set; }
    }

    public enum EMessageType
    {
        Unknown,
        FinishedCommandExecution,
        ExceptionCommandExecution,
        CallRequest,
        IdAssigning,
        CancelRequest,
        EventRequest,
        ClientRecovery
    }
}
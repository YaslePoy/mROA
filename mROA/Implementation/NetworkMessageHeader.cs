using System;
using System.Text.Json.Serialization;
using mROA.Abstract;
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
        public NetworkMessageHeader()
        {
            Id = Guid.NewGuid();
            MessageType = EMessageType.Unknown;
            Data = Array.Empty<byte>();
        }
        public NetworkMessageHeader(ISerializationToolkit serializationToolkit, INetworkMessage networkMessage)
        {
            MessageType = networkMessage.MessageType;
            Data = serializationToolkit.Serialize(networkMessage);
        }
        public Guid Id { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EMessageType MessageType { get; set; }

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
        ClientRecovery,
        ClientConnect
    }
}
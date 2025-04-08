using System;
using System.Text.Json.Serialization;
using mROA.Abstract;

// ReSharper disable UnusedMember.Global

namespace mROA.Implementation
{
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
            Id = Guid.NewGuid();
        }
        public Guid Id { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EMessageType MessageType { get; set; }

        public byte[] Data { get; set; }
    }
}
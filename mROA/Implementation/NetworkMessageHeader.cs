using System;
using mROA.Abstract;

// ReSharper disable UnusedMember.Global

namespace mROA.Implementation
{
    public class NetworkMessageHeader
    {
        private bool Equals(NetworkMessageHeader other)
        {
            return Id.Equals(other.Id) && MessageType == other.MessageType;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((NetworkMessageHeader)obj);
        }

        public static readonly NetworkMessageHeader Null = new();
        public NetworkMessageHeader()
        {
            Id = Guid.NewGuid();
            MessageType = EMessageType.Unknown;
            Data = Array.Empty<byte>();
        }
        public NetworkMessageHeader(IContextualSerializationToolKit serializationToolkit,
            INetworkMessage networkMessage, IEndPointContext? context)
        {
            MessageType = networkMessage.MessageType;
            Data = serializationToolkit.Serialize(networkMessage, context);
            Id = Guid.NewGuid();
        }
        public Guid Id { get; set; }

        public EMessageType MessageType { get; set; }

        public byte[] Data { get; set; }
    }
}
using System;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class NetworkMessage
    {
        private bool Equals(NetworkMessage other)
        {
            return Id.Equals(other.Id) && MessageType == other.MessageType;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((NetworkMessage)obj);
        }

        public NetworkMessage()
        {
            Data = Array.Empty<byte>();
        }

        public NetworkMessage(IContextualSerializationToolKit serializationToolkit,
            INetworkMessage networkMessage, IEndPointContext? context)
        {
            MessageType = networkMessage.MessageType;
            Data = serializationToolkit.Serialize(networkMessage, context);
            Id = Guid.NewGuid();
        }

        public Guid Id { get; set; }

        public EMessageType MessageType { get; set; }

        public byte[] Data { get; set; }

        public override string ToString()
        {
            return $" {Id}:{MessageType} [{Data.Length}]";
        }
        
        public struct NetworkMessageMeta
        {
            public byte Type;
            public Guid Id;
            public ushort BodyLength;

            public NetworkMessageMeta(ReadOnlySpan<byte> metadata)
            {
                Type = metadata[0];
                Id = new Guid(metadata[1..17]);
                BodyLength = BitConverter.ToUInt16(metadata[17..]);
            }

            public NetworkMessage ToMessage(ReadOnlySpan<byte> memory)
            {
                var data = memory[19..][..BodyLength];
                return new NetworkMessage
                {
                    Data = data.ToArray(),
                    Id = Id,
                    MessageType = (EMessageType)Type
                };
            }
        }
    }
}
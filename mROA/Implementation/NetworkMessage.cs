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
            Id = RequestId.Generate();
        }

        public RequestId Id { get; set; }

        public EMessageType MessageType { get; set; }

        public byte[] Data { get; set; }
        public object Serialized { get; set; }
        public IEndPointContext Context { get; set; }

        public override string ToString()
        {
            return $" {Id}:{MessageType} [{Data.Length}]";
        }

        public NetworkMessageMeta ToMeta()
        {
            return new NetworkMessageMeta
            {
                BodyLength = (ushort)(Data == null ? 0 : Data.Length),
                Type = (byte)MessageType,
                Id = Id
            };
        }

        public struct NetworkMessageMeta
        {
            public RequestId Id;
            public ushort BodyLength;
            public byte Type;

            public NetworkMessage ToMessage(Span<byte> memory)
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
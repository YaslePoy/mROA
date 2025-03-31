using mROA.Implementation.Attributes;

namespace mROA.Implementation
{
    public interface INetworkMessage
    {
        [SerializationIgnore]
        public EMessageType MessageType { get; }
    }
}
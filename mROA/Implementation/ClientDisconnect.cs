namespace mROA.Implementation
{
    public class ClientDisconnect : INetworkMessage
    {
        public EMessageType MessageType => EMessageType.ClientDisconnect;
    }
}
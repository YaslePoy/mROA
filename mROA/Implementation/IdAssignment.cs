namespace mROA.Implementation
{
    public class IdAssignment : INetworkMessage
    {
        public int Id { get; set; }

        public EMessageType MessageType => EMessageType.IdAssigning;
    }

    public class ClientRecovery : INetworkMessage
    {
        public int Id { get; set; }
        public EMessageType MessageType => EMessageType.ClientRecovery;
    }

    public class ClientConnect : INetworkMessage
    {
        public EMessageType MessageType => EMessageType.ClientConnect;
    }
}
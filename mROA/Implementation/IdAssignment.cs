namespace mROA.Implementation
{
    public class IdAssignment : INetworkMessage
    {
        public int Id { get; set; }
        public CallIndexConfig IndexConfig { get; set; }
        public EMessageType MessageType => EMessageType.IdAssigning;
    }

    public class ClientRecovery : INetworkMessage
    {
        // ReSharper disable once UnusedMember.Global
        public ClientRecovery()
        {
            Id = 0;
        }

        public ClientRecovery(int id)
        {
            Id = id;
        }

        public int Id { get; set; }
        public EMessageType MessageType => EMessageType.ClientRecovery;
    }

    public class ClientConnect : INetworkMessage
    {
        public EMessageType MessageType => EMessageType.ClientConnect;
    }
}
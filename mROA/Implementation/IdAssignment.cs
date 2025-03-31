namespace mROA.Implementation
{
    public class IdAssignment : INetworkMessage
    {
        public int Id { get; set; }

        public EMessageType MessageType => EMessageType.IdAssigning;
    }
}
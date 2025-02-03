namespace mROA;

public interface IInteractionModule
{
    void SetMessageHandler(Action<int, byte[]> handler);
    void SendTo(int clientId, byte[] message);
    public interface IFrontendInteractionModule
    {
        public Task<byte[]> ReceiveMessage();
        public void PostMessage(byte[] message);
    }
}
namespace mROA;

public interface IInteractionModule
{
    void SetMessageHandler(Action<int, byte[]> handler);
    void SendTo(int clientId, byte[] message);
    void RegisterSourse(Stream stream);
    public interface IFrontendInteractionModule
    {
        public Task<byte[]> ReceiveMessage();
        public void PostMessage(byte[] message);
    }
}
namespace mROA.Abstract;

public interface IInteractionModule : IInjectableModule
{
    void SendTo(int clientId, byte[] message);
    void RegisterSource(Stream stream);
    public interface IFrontendInteractionModule : IInjectableModule
    {
        int ClientId { get; }
        public Task<byte[]> ReceiveMessage();
        public void PostMessage(byte[] message);
    }
}
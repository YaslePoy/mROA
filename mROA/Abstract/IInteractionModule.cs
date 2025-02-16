using mROA.Implementation;

namespace mROA.Abstract;

public interface IInteractionModule : IInjectableModule
{
    void SendTo(int clientId, byte[] message);
    void RegisterSource(Stream stream);
    Stream GetSource(int clientId);
    public interface IFrontendInteractionModule : IInjectableModule
    {
        int ClientId { get; }
        public Task<byte[]> ReceiveMessage();
        public void PostMessage(byte[] message);
    }
}

public interface INextGenerationInteractionModule : IInjectableModule
{
    int ConntectionId { get; }
    public Stream? BaseStream { get; set; }
    Task<NetworkMessage> GetNextMessageReceiving();
    void PostMessage(NetworkMessage message);
    NetworkMessage[] UnhandledMessages { get; }
    void HandleMessage(NetworkMessage msg);
}
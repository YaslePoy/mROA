using mROA.Abstract;

namespace mROA;

public interface IInteractionModule : IInjectableModule
{
    void SendTo(int clientId, byte[] message);
    void RegisterSourse(Stream stream);
    public interface IFrontendInteractionModule : IInjectableModule
    {
        public Task<byte[]> ReceiveMessage();
        public void PostMessage(byte[] message);
    }
}
using mROA.Implementation;

namespace mROA.Abstract;

public interface INextGenerationInteractionModule : IInjectableModule
{
    int ConnectionId { get; }
    public Stream? BaseStream { get; set; }
    Task<NetworkMessage> GetNextMessageReceiving();
    Task PostMessage(NetworkMessage message);
    void HandleMessage(NetworkMessage message);
    NetworkMessage[] UnhandledMessages { get; }
}
using mROA.Implementation;

namespace mROA.Abstract;

public interface INextGenerationInteractionModule : IInjectableModule
{
    int ConnectionId { get; }
    public Stream BaseStream { get; set; }
    NetworkMessage[] UnhandledMessages { get; }
    NetworkMessage LastMessage { get; }
    EventWaitHandle CurrentReceivingHandle { get; }
    void StartInfiniteReceiving();
    Task<NetworkMessage> GetNextMessageReceiving();
    Task PostMessage(NetworkMessage message);
    void HandleMessage(NetworkMessage message);
    NetworkMessage? FirstByFilter(Predicate<NetworkMessage> predicate);
}
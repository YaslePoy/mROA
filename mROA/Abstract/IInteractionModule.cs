using mROA.Implementation;

namespace mROA.Abstract;

public interface INextGenerationInteractionModule : IInjectableModule
{
    int ConntectionId { get; }
    public Stream? BaseStream { get; set; }
    Task<NetworkMessage> GetNextMessageReceiving();
    Task PostMessage(NetworkMessage message);
}
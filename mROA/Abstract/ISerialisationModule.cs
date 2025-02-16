using System.Windows.Input;
using mROA.Implementation;

namespace mROA.Abstract;

public interface ISerialisationModule : IInjectableModule
{
    void HandleIncomingRequest(int clientId, byte[] message);
    void PostResponse(NetworkMessage message, int clientId);
    void SendWelcomeMessage(int clientId);
    public interface IFrontendSerialisationModule : IInjectableModule
    {
        int ClientId { get; }
        Task<T> GetNextCommandExecution<T>(Guid requestId) where T : ICommandExecution;
        Task<FinalCommandExecution<T>> GetFinalCommandExecution<T>(Guid requestId);
        void PostCallRequest(ICallRequest callRequest);
    }
}

public interface IRepresentationModule : IInjectableModule
{
    int Id { get; }
    Task<T> GetMessage<T>(Guid? requestId, MessageType? messageType);
    Task PostCallMessage<T>(Guid id, MessageType messageType, T payload);
    Task PostCallMessage(Guid id, MessageType messageType, object payload, Type payloadType);
}
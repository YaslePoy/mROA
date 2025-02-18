using mROA.Implementation;
using mROA.Implementation.CommandExecution;

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
    Task<T> GetMessageAsync<T>(Guid? requestId = null, MessageType? messageType = null);
    T GetMessage<T>(Guid? requestId = null, MessageType? messageType = null);
    Task<byte[]> GetRawMessage(Guid? requestId = null, MessageType? messageType = null);
    
    Task PostCallMessageAsync<T>(Guid id, MessageType messageType, T payload) where T : notnull;
    Task PostCallMessageAsync(Guid id, MessageType messageType, object payload, Type payloadType);
    void PostCallMessage<T>(Guid id, MessageType messageType, T payload) where T : notnull;
    void PostCallMessage(Guid id, MessageType messageType, object payload, Type payloadType); 
}
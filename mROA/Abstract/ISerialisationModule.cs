using mROA.Implementation;

namespace mROA.Abstract;

public interface ISerialisationModule : IInjectableModule
{
    void HandleIncomingRequest(int clientId, byte[] message);
    void PostResponse(NetworkMessage message, int clientId);
    public interface IFrontendSerialisationModule : IInjectableModule
    {
        Task<T> GetNextCommandExecution<T>(Guid requestId) where T : ICommandExecution;
        Task<FinalCommandExecution<T>> GetFinalCommandExecution<T>(Guid requestId);
        
        void PostCallRequest(ICallRequest callRequest);
    }
}
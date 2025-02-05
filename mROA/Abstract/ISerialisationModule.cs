using mROA.Abstract;
using mROA.Implementation;

namespace mROA;

public interface ISerialisationModule : IInjectableModule
{
    void HandleIncomingRequest(int clientId, byte[] command);
    void PostResponse(ICommandExecution call);
    public interface IFrontendSerialisationModule : IInjectableModule
    {
        Task<T> GetNextCommandExecution<T>(Guid requestId) where T : ICommandExecution;
        Task<FinalCommandExecution<T>> GetFinalCommandExecution<T>(Guid requestId);
        
        void PostCallRequest(ICallRequest callRequest);
    }
}
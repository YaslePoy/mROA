using mROA.Implementation;

namespace mROA;

public interface ISerialisationModule
{
    void HandleIncomingRequest(int clientId, byte[] command);
    void PostResponse(ICommandExecution call);
    void SetExecuteModule(IExecuteModule executeModule);
    public interface IFrontendSerialisationModule
    {
        Task<T> GetNextCommandExecution<T>(Guid requestId) where T : ICommandExecution;
        Task<FinalCommandExecution> GetFinalCommandExecution<T>(Guid requestId);
        
        void PostCallRequest(ICallRequest callRequest);
    }
}
using mROA.Implementation;

namespace mROA;

public interface ISerialisationModule
{
    void HandleIncomingRequest(int clientId, byte[] command);
    void PostResponse(ICommandExecution call);
    void SetExecuteModule(IExecuteModule executeModule);
    public interface IFrontendSerialisationModule
    {
        ICommandExecution GetNextCommandExecution<T>(Guid requestId) where T : ICommandExecution;
        void PostCallRequest(ICallRequest callRequest);
    }
}
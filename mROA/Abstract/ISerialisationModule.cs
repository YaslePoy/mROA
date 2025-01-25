using mROA.Implementation;

namespace mROA;

public interface ISerialisationModule
{
    void HandleIncomingRequest(int clientId, byte[] command);
    void PostResponse(ICommandExecution call);
    void SetExecuteModule(IExecuteModule executeModule);
    public interface IFrontendSerialisationModule
    {
        ICommandExecution GetNextCommandExecution(Guid requestId);
        void PostCallRequest(ICallRequest callRequest);
    }
}
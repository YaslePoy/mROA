using mROA.Implementation;

namespace mROA;

public interface ISerialisationModule
{
    void HandleIncomingRequest(int clientId, string command);
    void PostResponse(ICommandExecution call);
    void SetExecuteModule(IExecuteModule executeModule);
}
using mROA.Implementation;

namespace mROA;

public interface IInputModule
{
    void HandleIncomingRequest(int clientId, string command);
    void PostResponse(ICommandExecution call);
}
namespace mROA;

public interface IInputModule
{
    void HandleIncomingRequest(object command);
    void PostResponse(ICommandExecution call);
}
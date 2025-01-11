namespace mROA;

public interface IInputModule
{
    void AddIncomingRequestHandler(Action<object> handler);
    void SendResponse(ICommandExecution call, object response);
}
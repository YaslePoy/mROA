using mROA.Implementation;

namespace mROA;

public interface ISerialisationModule
{
    void HandleIncomingRequest(int clientId, string command);
    void PostResponse(ICommandExecution call);
    void SetExecuteModule(IExecuteModule executeModule);
    public interface IFrontendSerialisationModule
    {
        void HandlePostedResponse(string response);
        void PostCallRequest(ICallRequest callRequest);
        Task<T> GetCommandExecutionResult<T>(Guid callRequestId, CancellationToken cancellationToken);
    }
}
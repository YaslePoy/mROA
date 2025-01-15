using System.Text.Json;

namespace mROA.Implementation;

public class JsonFrontendSerialisationModule : ISerialisationModule.IFrontendSerialisationModule
{
    public void HandlePostedResponse(string response)
    {
        
    }
    
    
    public void PostCallRequest(ICallRequest callRequest)
    {
        
    }

    public async Task<T> GetCommandExecutionResult<T>(Guid callRequestId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
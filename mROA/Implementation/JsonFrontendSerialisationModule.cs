using System.Text.Json;

namespace mROA.Implementation;

public class JsonFrontendSerialisationModule : ISerialisationModule.IFrontendSerialisationModule
{
    public ICommandExecution GetNextCommandExecution(Guid requestId)
    {
        return null;
    }

    public void PostCallRequest(ICallRequest callRequest)
    {
        
    }
}
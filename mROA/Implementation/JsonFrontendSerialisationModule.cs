using System.Text;
using System.Text.Json;

namespace mROA.Implementation;

public class JsonFrontendSerialisationModule(IInteractionModule.IFrontendInteractionModule interactionModule)
    : ISerialisationModule.IFrontendSerialisationModule
{
    public ICommandExecution GetNextCommandExecution<T>(Guid requestId) where T : ICommandExecution
    {
        var receiveMessage = interactionModule.ReceiveMessage();
        var parsed = JsonSerializer.Deserialize<T>(receiveMessage);
        while (parsed.CallRequestId != requestId)
        {
            receiveMessage = interactionModule.ReceiveMessage();
            parsed = JsonSerializer.Deserialize<T>(receiveMessage);
        }
        return parsed;
    }

    public void PostCallRequest(ICallRequest callRequest)
    {
        var post = JsonSerializer.Serialize(callRequest, callRequest.GetType());
        interactionModule.PostMessage(Encoding.UTF8.GetBytes(post));
    }
}
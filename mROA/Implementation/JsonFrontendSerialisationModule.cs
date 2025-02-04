using System.Text;
using System.Text.Json;

namespace mROA.Implementation;

public class JsonFrontendSerialisationModule(IInteractionModule.IFrontendInteractionModule interactionModule)
    : ISerialisationModule.IFrontendSerialisationModule
{
    public async Task<T> GetNextCommandExecution<T>(Guid requestId) where T : ICommandExecution
    {
        var receiveMessage = await interactionModule.ReceiveMessage();
        var parsed = JsonSerializer.Deserialize<T>(receiveMessage);
        while (parsed.CallRequestId != requestId)
        {
            receiveMessage = await interactionModule.ReceiveMessage();
            parsed = JsonSerializer.Deserialize<T>(receiveMessage);
        }
        return parsed;
    }

    public async Task<FinalCommandExecution> GetFinalCommandExecution<T>(Guid requestId)
    {
        var receiveMessage = await interactionModule.ReceiveMessage();
        var parsed = JsonSerializer.Deserialize<FinalCommandExecution>(receiveMessage);
        while (parsed.CallRequestId != requestId)
        {
            receiveMessage = await interactionModule.ReceiveMessage();
            parsed = JsonSerializer.Deserialize<FinalCommandExecution>(receiveMessage);
        }
        parsed.Result = parsed.Result! is JsonElement e ? e.Deserialize<T>() : (T)parsed.Result!;
        return parsed;
    }

    public void PostCallRequest(ICallRequest callRequest)
    {
        var post = JsonSerializer.Serialize(callRequest, callRequest.GetType());
        interactionModule.PostMessage(Encoding.UTF8.GetBytes(post));
    }
}
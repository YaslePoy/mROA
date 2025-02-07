using System.Text;
using System.Text.Json;

namespace mROA.Implementation;

public class JsonFrontendSerialisationModule
    : ISerialisationModule.IFrontendSerialisationModule
{
    private IInteractionModule.IFrontendInteractionModule _interactionModule;

    public async Task<T> GetNextCommandExecution<T>(Guid requestId) where T : ICommandExecution
    {
        var receiveMessage = await _interactionModule.ReceiveMessage();
        var parsed = JsonSerializer.Deserialize<T>(receiveMessage);
        while (parsed.CallRequestId != requestId)
        {
            receiveMessage = await _interactionModule.ReceiveMessage();
            parsed = JsonSerializer.Deserialize<T>(receiveMessage);
        }

        return parsed;
    }

    public async Task<FinalCommandExecution<T>> GetFinalCommandExecution<T>(Guid requestId)
    {
        var receiveMessage = await _interactionModule.ReceiveMessage();
        // var str = Encoding.UTF8.GetString(receiveMessage);
        var document = JsonDocument.Parse(receiveMessage);
        if (document.RootElement.TryGetProperty("Exeption", out var exeptionElement))
        {
            throw new RemoteException(exeptionElement.GetString()) { CallRequestId = requestId };
        }

        var parsed = document.Deserialize<FinalCommandExecution<T>>();
        while (parsed.CallRequestId != requestId)
        {
            receiveMessage = await _interactionModule.ReceiveMessage();
            parsed = JsonSerializer.Deserialize<FinalCommandExecution<T>>(receiveMessage);
        }

        parsed.Result = parsed.Result! is JsonElement e ? e.Deserialize<T>() : (T)parsed.Result!;
        return parsed;
    }

    public void PostCallRequest(ICallRequest callRequest)
    {
        var post = JsonSerializer.Serialize(callRequest, callRequest.GetType());
        _interactionModule.PostMessage(Encoding.UTF8.GetBytes(post));
    }

    public void Inject<T>(T dependency)
    {
        if (dependency is IInteractionModule.IFrontendInteractionModule interactionModule)
            _interactionModule = interactionModule;
    }

}

public class RemoteException(string error) : Exception
{
    public Guid CallRequestId;
    public override string Message => error;
}
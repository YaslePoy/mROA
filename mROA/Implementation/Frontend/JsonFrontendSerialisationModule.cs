using System.Text;
using System.Text.Json;
using mROA.Abstract;

namespace mROA.Implementation.Frontend;

public class JsonFrontendSerialisationModule
    : ISerialisationModule.IFrontendSerialisationModule
{
    private IInteractionModule.IFrontendInteractionModule? _interactionModule;

    public async Task<T> GetNextCommandExecution<T>(Guid requestId) where T : ICommandExecution
    {
        if (_interactionModule is null)
            throw new Exception("Interaction module not initialized");
        
        var receiveMessage = await _interactionModule.ReceiveMessage();
        var parsed = JsonSerializer.Deserialize<T>(receiveMessage)!;
        while (parsed.CallRequestId != requestId)
        {
            receiveMessage = await _interactionModule.ReceiveMessage();
            parsed = JsonSerializer.Deserialize<T>(receiveMessage)!;
        }

        return parsed;
    }

    public async Task<FinalCommandExecution<T>> GetFinalCommandExecution<T>(Guid requestId)
    {
        if (_interactionModule is null)
            throw new Exception("Interaction module not initialized");
        
        var receiveMessage = await _interactionModule.ReceiveMessage();
        // var str = Encoding.UTF8.GetString(receiveMessage);
        var document = JsonDocument.Parse(receiveMessage);
        if (document.RootElement.TryGetProperty("Exception", out var exceptionElement))
        {
            throw new RemoteException(exceptionElement.GetString()!) { CallRequestId = requestId };
        }

        var parsed = document.Deserialize<FinalCommandExecution<T>>()!;
        while (parsed.CallRequestId != requestId)
        {
            receiveMessage = await _interactionModule.ReceiveMessage();
            parsed = JsonSerializer.Deserialize<FinalCommandExecution<T>>(receiveMessage)!;
        }

        return parsed;
    }

    public void PostCallRequest(ICallRequest callRequest)
    {
        if (_interactionModule is null)
            throw new Exception("Interaction module not initialized");
        
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
    public override string Message => $"Error in request {CallRequestId} : {error}";
}
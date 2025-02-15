using System.Text;
using System.Text.Json;
using mROA.Abstract;

namespace mROA.Implementation.Frontend;

public class JsonFrontendSerialisationModule
    : ISerialisationModule.IFrontendSerialisationModule
{
    private IInteractionModule.IFrontendInteractionModule? _interactionModule;
    public int ClientId => _interactionModule!.ClientId;

    public async Task<T> GetNextCommandExecution<T>(Guid requestId) where T : ICommandExecution
    {
        if (_interactionModule is null)
            throw new Exception("Interaction module not initialized");

        var receiveMessage = await _interactionModule.ReceiveMessage();
        var message = JsonSerializer.Deserialize<NetworkMessage>(receiveMessage)!;

        while (message.Id != requestId)
        {
            receiveMessage = await _interactionModule.ReceiveMessage();
            message = JsonSerializer.Deserialize<NetworkMessage>(receiveMessage)!;
        }

        var parsed = JsonSerializer.Deserialize<T>(message.Data)!;

        if (message.SchemaId == MessageType.ErrorCommandExecution)
        {
            throw new RemoteException(JsonSerializer.Deserialize<ExceptionCommandExecution>(message.Data)!.Exception)
                { CallRequestId = requestId };
        }

        return parsed;
    }

    public async Task<FinalCommandExecution<T>> GetFinalCommandExecution<T>(Guid requestId)
    {
        if (_interactionModule is null)
            throw new Exception("Interaction module not initialized");

        var receiveMessage = await _interactionModule.ReceiveMessage();

        var message = JsonSerializer.Deserialize<NetworkMessage>(receiveMessage)!;
        while (message.Id != requestId)
        {
            receiveMessage = await _interactionModule.ReceiveMessage();

            message = JsonSerializer.Deserialize<NetworkMessage>(receiveMessage)!;
        }

        if (message.SchemaId == MessageType.ErrorCommandExecution)
        {
            throw new RemoteException(JsonSerializer.Deserialize<ExceptionCommandExecution>(message.Data)!.Exception)
                { CallRequestId = requestId };
        }

        return JsonSerializer.Deserialize<FinalCommandExecution<T>>(message.Data)!;
    }

    public void PostCallRequest(ICallRequest callRequest)
    {
        if (_interactionModule is null)
            throw new Exception("Interaction module not initialized");


        var post = JsonSerializer.SerializeToUtf8Bytes(callRequest, callRequest.GetType());
        _interactionModule.PostMessage(JsonSerializer.SerializeToUtf8Bytes(new NetworkMessage
        {
            Id = callRequest.CallRequestId,
            Data = post,
            SchemaId = MessageType.CallRequest
        }));
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
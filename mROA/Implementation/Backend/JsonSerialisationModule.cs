using System.Text.Json;
using mROA.Abstract;

namespace mROA.Implementation.Backend;

public class JsonSerialisationModule : ISerialisationModule
{
    private IInteractionModule? _dataSource;
    private IExecuteModule? _executeModule;
    private IMethodRepository? _methodRepository;
    private IContextRepository? _contextRepo;

    public void HandleIncomingRequest(int clientId, byte[] message)
    {
        var ownership = TransmissionConfig.OwnershipRepository as MultiClientOwnershipRepository ?? throw new Exception("Set ownership repository type is incorrect");
        ownership.RegisterOwnership(clientId);
        NetworkMessage input = JsonSerializer.Deserialize<NetworkMessage>(message)!;
        
        if (input.SchemaId == MessageType.CallRequest)
        {
            var command = JsonSerializer.Deserialize<DefaultCallRequest>(input.Data)!;
            if (command.Parameter is not null)
            {
                var parameter = _methodRepository!.GetMethod(command.CommandId).GetParameters().First().ParameterType;
                var jsElement = (JsonElement)command.Parameter;
                command.Parameter = jsElement.Deserialize(parameter);
            }

            var response = _executeModule!.Execute(command, _contextRepo);
            response.ClientId = clientId;
            var resultType = response is FinalCommandExecution
                ? MessageType.FinishedCommandExecution
                : MessageType.ErrorCommandExecution;
            PostResponse(
                new NetworkMessage
                {
                    SchemaId = resultType,
                    Id = command.CallRequestId,
                    Data = JsonSerializer.SerializeToUtf8Bytes(response, response.GetType())
                }, clientId);
        }
        ownership.FreeOwnership();
    }

    public void PostResponse(NetworkMessage message, int clientId)
    {
        _dataSource!.SendTo(clientId, JsonSerializer.SerializeToUtf8Bytes(message));
    }

    public void SendWelcomeMessage(int clientId)
    {
        _dataSource!.SendTo(clientId, JsonSerializer.SerializeToUtf8Bytes(new NetworkMessage { Data = JsonSerializer.SerializeToUtf8Bytes(new IdAssingnment { Id = clientId }), SchemaId = MessageType.IdAssigning}));
    }

    public void Inject<T>(T dependency)
    {
        switch (dependency)
        {
            case IInteractionModule interactionModule:
                _dataSource = interactionModule;
                break;
            case IExecuteModule executeModule:
                _executeModule = executeModule;
                break;
            case IMethodRepository methodRepository:
                _methodRepository = methodRepository;
                break;
            case IContextRepository contextRepository:
                _contextRepo = contextRepository;
                break;
        }
    }
}
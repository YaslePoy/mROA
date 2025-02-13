using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using mROA.Abstract;

namespace mROA.Implementation.Backend;

public class JsonSerialisationModule : ISerialisationModule
{
    private IInteractionModule? _dataSource;
    private IExecuteModule? _executeModule;
    private IMethodRepository? _methodRepository;

    public void HandleIncomingRequest(int clientId, byte[] message)
    {
        NetworkMessage input = JsonSerializer.Deserialize<NetworkMessage>(message)!;

        if (input.SchemaId == MessageType.CallRequest)
        {
            var command = JsonSerializer.Deserialize<DefaultCallRequest>(input.Data);
            if (command.Parameter is not null)
            {
                var parameter = _methodRepository!.GetMethod(command.CommandId).GetParameters().First().ParameterType;
                command.Parameter = ((JsonElement)command.Parameter).Deserialize(parameter);
            }

            var response = _executeModule!.Execute(command);
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
    }

    public void PostResponse(NetworkMessage message, int clientId)
    {
        _dataSource!.SendTo(clientId, JsonSerializer.SerializeToUtf8Bytes(message));
    }

    public void SendWelcomeMessage(int clientId)
    {
        _dataSource.SendTo(clientId, JsonSerializer.SerializeToUtf8Bytes(new NetworkMessage { Data = JsonSerializer.SerializeToUtf8Bytes(new IdAssingnment { Id = clientId }), SchemaId = MessageType.IdAssigning}));
    }

    public void Inject<T>(T dependency)
    {
        if (dependency is IInteractionModule interactionModule)
            _dataSource = interactionModule;
        if (dependency is IExecuteModule executeModule)
            _executeModule = executeModule;
        if (dependency is IMethodRepository methodRepository)
            _methodRepository = methodRepository;
    }
}
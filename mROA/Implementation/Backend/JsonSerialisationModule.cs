using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace mROA.Implementation;

public class JsonSerialisationModule : ISerialisationModule
{
    private readonly IInteractionModule _dataSource;
    private IExecuteModule _executeModule;
    private IMethodRepository _methodRepository;
    public JsonSerialisationModule(IInteractionModule dataSource, IMethodRepository methodRepository)
    {
        _dataSource = dataSource;
        _methodRepository = methodRepository;
        _dataSource.SetMessageHandler(HandleIncomingRequest);
    } 

    public void SetExecuteModule(IExecuteModule executeModule) => _executeModule = executeModule;
    
    public void HandleIncomingRequest(int clientId, byte[] command)
    {
        DefaultCallRequest request = JsonSerializer.Deserialize<DefaultCallRequest>(command);

        if (request.Parameter is not null)
        {
            var parameter = _methodRepository.GetMethod(request.CommandId).GetParameters().First().ParameterType;
            request.Parameter = (( JsonElement)request.Parameter).Deserialize(parameter);
        }
        
        var response = _executeModule.Execute(request);
        response.ClientId = clientId;
        PostResponse(response);
    }

    public void PostResponse(ICommandExecution call)
    {
        var texted = string.Empty;
        if (call is FinalCommandExecution finalCommand)
        {
            texted = JsonSerializer.Serialize(finalCommand);
        }else if (call is AsyncCommandExecution asyncCommand)
        {
            texted = JsonSerializer.Serialize(asyncCommand);
        }
        var binary = Encoding.UTF8.GetBytes(texted);
        _dataSource.SendTo(call.ClientId, binary);
    }
}
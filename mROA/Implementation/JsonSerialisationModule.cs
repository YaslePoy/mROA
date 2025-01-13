using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace mROA.Implementation;

public class JsonSerialisationModule : ISerialisationModule
{
    private readonly IInteractionModule _dataSource;
    private IExecuteModule _executeModule;
    public JsonSerialisationModule(IInteractionModule dataSource)
    {
        _dataSource = dataSource;
        _dataSource.SetMessageHandler(HandleIncomingRequest);
    }

    public void SetExecuteModule(IExecuteModule executeModule) => _executeModule = executeModule;
    
    public void HandleIncomingRequest(int clientId, string command)
    {
        

        JsonCallRequest request = JsonSerializer.Deserialize<JsonCallRequest>(command);

        

        request.ClientId = clientId;
        var response = _executeModule.Execute(request);
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
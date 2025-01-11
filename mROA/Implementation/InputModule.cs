using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace mROA.Implementation;

public class InputModule : IInputModule
{
    private readonly IInteractionModule _dataSource;
    private readonly IExecuteModule _executeModule;

    public InputModule(IInteractionModule dataSource, IExecuteModule executeModule)
    {
        _dataSource = dataSource;
        _executeModule = executeModule;
        _dataSource.SetMessageHandler(HandleIncomingRequest);
    }

    public void HandleIncomingRequest(int clientId, string command)
    {
        var type = JsonDocument.Parse(command).RootElement.GetProperty("RequestTypeId").GetInt32();

        ICallRequest? request;

        switch (type)
        {
            case 0:
                request = JsonSerializer.Deserialize<StaticCallRequest>(command);
                break;
            case 1:
                request = JsonSerializer.Deserialize<CallRequest>(command);
                break;
            case 2:
                request = JsonSerializer.Deserialize<ParametrizedCallRequest>(command);
                break;
            default:
                request = JsonSerializer.Deserialize<StaticCallRequest>(command);
                break;
        }

        request.ClientId = clientId;
        var response = _executeModule.Execute(request);
        var texted = string.Empty;
        if (response is FinalCommandExecution finalCommand)
        {
            texted = JsonSerializer.Serialize(finalCommand);
        }else if (response is AsyncCommandExecution asyncCommand)
        {
            texted = JsonSerializer.Serialize(asyncCommand);
        }
        var binary = Encoding.UTF8.GetBytes(texted);
        _dataSource.SendTo(clientId, binary);
    }

    public void PostResponse(ICommandExecution call)
    {
        var texted = JsonSerializer.Serialize(call);
        var binary = Encoding.UTF8.GetBytes(texted);
        _dataSource.SendTo(call.ClientId, binary);
    }
}
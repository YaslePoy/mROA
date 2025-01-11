using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace mROA.Implementation;

public class InputModule(IInteractionModule dataSource, IExecuteModule executeModule)
    : IInputModule
{
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

        var response = executeModule.Execute(request);
        
        var texted = JsonSerializer.Serialize(response);
        var binary = Encoding.UTF8.GetBytes(texted);
        dataSource.SendTo(clientId, binary);
    }

    public void PostResponse(ICommandExecution call)
    {
        var texted = JsonSerializer.Serialize(call);
        var binary = Encoding.UTF8.GetBytes(texted);
        dataSource.SendTo(call.ClientId, binary);
    }
}
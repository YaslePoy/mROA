using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace mROA.Implementation;

public class InputModule(IInteractionModule dataSource, IExecuteModule executeModule)
    : IInputModule
{
    public void HandleIncomingRequest(object command)
    {
        
    }

    public void PostResponse(ICommandExecution call)
    {
        var texted = JsonSerializer.Serialize(call);
        var binary = Encoding.UTF8.GetBytes(texted);
        dataSource.SendTo(call.ClientId, binary);
    }
}
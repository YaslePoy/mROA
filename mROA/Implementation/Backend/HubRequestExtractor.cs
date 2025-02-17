using mROA.Abstract;

namespace mROA.Implementation.Backend;

public class HubRequestExtractor : IInjectableModule
{
    private IExecuteModule _executeModule;
    private IConnectionHub _hub;
    public void Inject<T>(T dependency)
    {
        switch (dependency)
        {
            case IExecuteModule executeModule:
                _executeModule = executeModule;
                break;
            case IConnectionHub connectionHub:
                _hub = connectionHub;
                _hub.OnConnected += HubOnOnConnected;
                break;
        }
    }

    private async void HubOnOnConnected(IRepresentationModule interaction)
    {
        try
        {
            while (true)
            {
                var messageReceiving = await interaction.GetMessage<DefaultCallRequest>(messageType: MessageType.CallRequest);
                
                
            }
        }
        catch (Exception e)
        {
            
        }
    }
}
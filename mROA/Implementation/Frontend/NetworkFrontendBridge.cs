using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using mROA.Abstract;

namespace mROA.Implementation.Frontend;

public class NetworkFrontendBridge(IPEndPoint ipEndPoint) : IFrontendBridge
{
    private readonly TcpClient _tcpClient = new();
    private StreamBasedFrontendInteractionModule? _interactionModule;

    public void Inject<T>(T dependency)
    {
        if (dependency is StreamBasedFrontendInteractionModule interactionModule)
        {
            _interactionModule = interactionModule;
        }
    }

    public void Connect()
    {
        if (_interactionModule is null)
            throw new Exception("Interaction module was not injected");
        
        _tcpClient.Connect(ipEndPoint);
        _interactionModule.ServerStream = _tcpClient.GetStream();
        var welcomeMessage = _interactionModule.ReceiveMessage().GetAwaiter().GetResult();
        var message = JsonSerializer.Deserialize<NetworkMessage>(welcomeMessage);
        if (message.SchemaId != MessageType.IdAssigning)
        {
            throw new Exception($"Incorrect message type. Must be IdAssigning, current : {message.SchemaId.ToString()}");
        }

        TransmissionConfig.OwnershipRepository = new StaticOwnershipRepository(JsonSerializer.Deserialize<IdAssingnment>(message.Data)!.Id);
    }
}
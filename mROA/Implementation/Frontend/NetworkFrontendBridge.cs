using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using mROA.Abstract;

namespace mROA.Implementation.Frontend;

public class NetworkFrontendBridge(IPEndPoint ipEndPoint) : IFrontendBridge
{
    private readonly TcpClient _tcpClient = new();
    private NextGenerationInteractionModule? _interactionModule;
    private ISerializationToolkit? _serialization;

    public void Inject<T>(T dependency)
    {
        switch (dependency)
        {
            case NextGenerationInteractionModule interactionModule:
                _interactionModule = interactionModule;
                break;
            case ISerializationToolkit toolkit:
                _serialization = toolkit;
                break;
        }
    }

    public void Connect()
    {
        if (_interactionModule is null)
            throw new Exception("Interaction module was not injected");
        
        _tcpClient.Connect(ipEndPoint);
        _interactionModule.BaseStream = _tcpClient.GetStream();
        var welcomeMessage = _interactionModule.GetNextMessageReceiving().GetAwaiter().GetResult();
        if (welcomeMessage.SchemaId != MessageType.IdAssigning)
        {
            throw new Exception($"Incorrect message type. Must be IdAssigning, current : {welcomeMessage.SchemaId.ToString()}");
        }

        TransmissionConfig.OwnershipRepository = new StaticOwnershipRepository(_serialization.Deserialize<IdAssingnment>(welcomeMessage.Data)!.Id);
    }
}
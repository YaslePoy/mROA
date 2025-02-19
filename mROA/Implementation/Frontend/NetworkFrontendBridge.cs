using System.Net;
using System.Net.Sockets;
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
        if (_serialization == null)
            throw new NullReferenceException("Serialization toolkit is not initialized");
        
        _tcpClient.Connect(ipEndPoint);
        _interactionModule.BaseStream = _tcpClient.GetStream();
        _interactionModule.StartInfiniteReceiving();

        var handle = _interactionModule.CurrentReceivingHandle;
        handle.WaitOne();
        var welcomeMessage = _interactionModule.LastMessage;
        if (welcomeMessage.MessageType != EMessageType.IdAssigning)
        {
            throw new Exception($"Incorrect message type. Must be IdAssigning, current : {welcomeMessage.MessageType.ToString()}");
        }
        _interactionModule.HandleMessage(welcomeMessage);
        TransmissionConfig.OwnershipRepository = new StaticOwnershipRepository(_serialization.Deserialize<IdAssingnment>(welcomeMessage.Data)!.Id);
    }
}
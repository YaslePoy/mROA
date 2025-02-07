using System.Net;
using System.Net.Sockets;
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
        _tcpClient.Connect(ipEndPoint);
        if (_interactionModule is null)
        {
            throw new Exception("Interaction module was not injected");
        }
        _interactionModule.ServerStream = _tcpClient.GetStream();
    }
}
using System.Net;
using System.Net.Sockets;
using mROA.Abstract;

namespace mROA.Implementation;

public class NetworkFrontendBridge : IFrontendBridge
{
    private TcpClient _tcpClient;
    public NetworkFrontendBridge(IPEndPoint serverEndPoint)
    {
        _tcpClient = new TcpClient();
        _tcpClient.Connect(serverEndPoint);
    }
    public void Inject<T>(T dependency)
    {
        if (dependency is StreamBasedFrontendInteractionModule interactionModule)
        {
            interactionModule.ServerStream = _tcpClient.GetStream();
        }
    }
}
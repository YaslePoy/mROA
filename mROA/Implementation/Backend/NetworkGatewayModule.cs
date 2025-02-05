using System.Net;
using System.Net.Sockets;

namespace mROA.Implementation;

public class NetworkGatewayModule : IGatewayModule
{
    private TcpListener _tcpListener;
    private IInteractionModule _interactionModule;

    public NetworkGatewayModule(IPEndPoint endpoint)
    {
        _tcpListener = new TcpListener(endpoint);
    }

    public void Run()
    {
        _tcpListener.Start();
        Console.WriteLine($"Listening on {_tcpListener.LocalEndpoint}");
        Console.WriteLine("Enter Backspace to stop");
        
        Task.Run(HandleIncomingConnections);

        while (true)
        {
            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.Backspace)
                break;
        }

        Console.WriteLine("Stopping");
        
        
    }

    public void Dispose()
    {
        _tcpListener.Stop();
        _tcpListener.Dispose();
    }

    private void HandleIncomingConnections()
    {
        while (true)
        {
            var client = _tcpListener.AcceptTcpClient();
            Console.WriteLine($"Client connected from {client.Client.RemoteEndPoint}");
            _interactionModule.RegisterSourse(client.GetStream());
            Console.WriteLine("Client registered");
        }
    }

    public void Inject<T>(T dependency)
    {
        if (dependency is IInteractionModule interactionModule)
            _interactionModule = interactionModule;
    }

    public void Bake()
    {
    }
}
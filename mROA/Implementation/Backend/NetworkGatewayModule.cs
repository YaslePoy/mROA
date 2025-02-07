using System.Net;
using System.Net.Sockets;
using mROA.Abstract;

namespace mROA.Implementation.Backend;

public class NetworkGatewayModule(IPEndPoint endpoint) : IGatewayModule
{
    private readonly TcpListener _tcpListener = new(endpoint);
    private IInteractionModule? _interactionModule;


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
        if (_interactionModule is null)
            throw new NullReferenceException("Interaction module is null");
        
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

}
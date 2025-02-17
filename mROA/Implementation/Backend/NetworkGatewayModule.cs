using System.Net;
using System.Net.Sockets;
using mROA.Abstract;

namespace mROA.Implementation.Backend;

public class NetworkGatewayModule() : IGatewayModule
{
    private readonly IPEndPoint? _endpoint;
    private readonly Type? _interactionModuleType;
    private readonly IInjectableModule[]? _injectableModules;
    private readonly TcpListener? _tcpListener;
    private IConnectionHub? _hub;

    public NetworkGatewayModule(IPEndPoint endpoint, Type interactionModuleType, IInjectableModule[] injectableModules)
    {
        _endpoint = endpoint;
        _tcpListener = new(_endpoint);

        _interactionModuleType = interactionModuleType;
        _injectableModules = injectableModules;
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
        if (_hub is null)
            throw new NullReferenceException("Hub module is null");
        
        if (_tcpListener == null)
            throw new NullReferenceException("TcpListener is null");
        
        if (_injectableModules is null)
            throw new NullReferenceException("InjectableModules is null");

        if (_interactionModuleType is null)
            throw new NullReferenceException("InteractionModuleType is null");
        
        while (true)
        {
            var client = _tcpListener.AcceptTcpClient();
            Console.WriteLine($"Client connected from {client.Client.RemoteEndPoint}");
            var interacton = Activator.CreateInstance(_interactionModuleType) as INextGenerationInteractionModule;
            foreach (var injectableModule in _injectableModules)
                interacton.Inject(injectableModule);
            _hub.RegisterInteracion(new NextGenerationInteractionModule());
            Console.WriteLine("Client registered");
        }
    }

    public void Inject<T>(T dependency)
    {
        if (dependency is IConnectionHub interactionModule)
            _hub = interactionModule;
    }
}
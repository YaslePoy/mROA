using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using mROA.Abstract;

namespace mROA.Implementation.Backend
{
    public class NetworkGatewayModule : IGatewayModule
    {
        private readonly Type? _interactionModuleType;
        private readonly IInjectableModule[]? _injectableModules;
        private readonly TcpListener _tcpListener;
        private IConnectionHub? _hub;
        private ISerializationToolkit? _serialization;

        public NetworkGatewayModule(IPEndPoint endpoint, Type interactionModuleType, IInjectableModule[] injectableModules)
        {
            _tcpListener = new(endpoint);
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
            if (_serialization is null)
                throw new NullReferenceException("Serialization is null");
        
            while (true)
            {
                var client = _tcpListener.AcceptTcpClient();
                Console.WriteLine($"Client connected from {client.Client.RemoteEndPoint}");
                var interaction = Activator.CreateInstance(_interactionModuleType) as INextGenerationInteractionModule;

                foreach (var injectableModule in _injectableModules)
                    interaction!.Inject(injectableModule);

                interaction!.Inject(_serialization);
            
                interaction.BaseStream = client.GetStream();
            
                interaction.PostMessage(new NetworkMessage
                {
                    Id = Guid.NewGuid(), SchemaId = MessageType.IdAssigning,
                    Data = _serialization.Serialize(new IdAssingnment { Id = interaction.ConnectionId })
                });
                _hub.RegisterInteraction(interaction);
                Console.WriteLine("Client registered");
            }
        }

        public void Inject<T>(T dependency)
        {
            if (dependency is IConnectionHub interactionModule)
                _hub = interactionModule;
            if (dependency is ISerializationToolkit serializationToolkit)
                _serialization = serializationToolkit;
        }
    }
}
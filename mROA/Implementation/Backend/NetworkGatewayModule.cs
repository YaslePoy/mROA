using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using mROA.Abstract;

namespace mROA.Implementation.Backend
{
    public class NetworkGatewayModule : IGatewayModule
    {
        private readonly IInjectableModule[]? _injectableModules;
        private readonly Type? _interactionModuleType;
        private readonly TcpListener _tcpListener;
        private IConnectionHub? _hub;
        private ISerializationToolkit? _serialization;

        public NetworkGatewayModule(IPEndPoint endpoint, Type interactionModuleType,
            IInjectableModule[] injectableModules)
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

        public void Inject<T>(T dependency)
        {
            switch (dependency)
            {
                case IConnectionHub interactionModule:
                    _hub = interactionModule;
                    break;
                case ISerializationToolkit serializationToolkit:
                    _serialization = serializationToolkit;
                    break;
            }
        }

        private void HandleIncomingConnections()
        {
            ThrowIfNotInjected();

            while (true)
            {
                var client = _tcpListener.AcceptTcpClient();
                Console.WriteLine($"Client connected from {client.Client.RemoteEndPoint}");
                var interaction = Activator.CreateInstance(_interactionModuleType!) as INextGenerationInteractionModule;
                foreach (var injectableModule in _injectableModules!)
                    interaction!.Inject(injectableModule);

                interaction!.Inject(_serialization);

                interaction.BaseStream = client.GetStream();
                
                var connectionRequest = interaction.GetNextMessageReceiving().GetAwaiter().GetResult()!;

                if (connectionRequest.MessageType == EMessageType.ClientConnect)
                {
                    interaction.PostMessageAsync(new NetworkMessageHeader(_serialization!,
                        new IdAssignment { Id = -interaction.ConnectionId }));
                    _hub!.RegisterInteraction(interaction);
                    Console.WriteLine("Client registered");
                }else if (connectionRequest.MessageType == EMessageType.ClientRecovery)
                {
                    var modidiedModules = _injectableModules!.ToList();
                    modidiedModules.RemoveAll(i => i is IIdentityGenerator);
                    
                    foreach (var injectableModule in modidiedModules)
                        interaction.Inject(injectableModule);

                    var recoveryRequest = _serialization!.Deserialize<ClientRecovery>(connectionRequest.Data)!;
                    interaction.ConnectionId = recoveryRequest.Id;
                    
                    interaction.Inject(_serialization);

                    interaction.BaseStream = client.GetStream();

                    interaction.PostMessageAsync(new NetworkMessageHeader(_serialization!,
                        new IdAssignment { Id = -interaction.ConnectionId }));
                    _hub!.RegisterInteraction(interaction);
                    Console.WriteLine("Client registered");
                }
            }
        }

        private void ThrowIfNotInjected()
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
        }
    }
}
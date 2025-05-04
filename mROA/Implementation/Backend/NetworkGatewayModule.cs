using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
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
        private IContextualSerializationToolKit? _serialization;
        private Dictionary<int, CancellationTokenSource> _extractorsCTS = new();

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
                case IContextualSerializationToolKit serializationToolkit:
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
                var interaction = Activator.CreateInstance(_interactionModuleType!) as IChannelInteractionModule;

                foreach (var injectableModule in _injectableModules!)
                    interaction!.Inject(injectableModule);

                interaction!.Inject(_serialization);


                //TODO сделать контекст
                var context = new EndPointContext();

                var streamExtractor =
                    new ChannelInteractionModule.StreamExtractor(client.GetStream(), _serialization, context);
                interaction.IsConnected = () => streamExtractor.IsConnected;
                streamExtractor.MessageReceived = message => { interaction.ReceiveChanel.Writer.WriteAsync(message); };
                streamExtractor.SingleReceive();
                var connectionRequest = interaction.GetNextMessageReceiving(false)
                    .GetAwaiter().GetResult()!;
                var cts = new CancellationTokenSource();

                switch (connectionRequest.MessageType)
                {
                    case EMessageType.ClientConnect:
                        context.HostId = 0;
                        context.OwnerId = interaction.ConnectionId;
                        Task.Run(async () => await streamExtractor.LoopedReceive(cts.Token));
                        _ = streamExtractor.SendFromChannel(interaction.TrustedPostChanel, cts.Token);
                        interaction.PostMessageAsync(new NetworkMessageHeader(_serialization!,
                            new IdAssignment { Id = -interaction.ConnectionId }, null));
                        _extractorsCTS[interaction.ConnectionId] = cts;
                        _hub!.RegisterInteraction(interaction);
                        Console.WriteLine("Client registered");
                        break;
                    case EMessageType.ClientRecovery:
                    {
                        var recoveryRequest = _serialization!.Deserialize<ClientRecovery>(connectionRequest.Data, null);
                        var recoveryInteraction = _hub.GetInteraction(recoveryRequest.Id);

                        _extractorsCTS[recoveryRequest.Id].Cancel();

                        recoveryInteraction.IsConnected = () => streamExtractor.IsConnected;
                        streamExtractor.MessageReceived = message =>
                        {
                            recoveryInteraction.ReceiveChanel.Writer.WriteAsync(message);
                        };
                        _ = streamExtractor.SendFromChannel(recoveryInteraction.TrustedPostChanel, cts.Token);

                        Task.Run(async () => await streamExtractor.LoopedReceive(cts.Token));


                        recoveryInteraction.Restart(false);
                        Console.WriteLine("Connection recovery for client {0} finished", recoveryRequest.Id);
                        break;
                    }
                    default:
                        client.Close();
                        break;
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
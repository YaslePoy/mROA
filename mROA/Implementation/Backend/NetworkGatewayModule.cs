using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using mROA.Abstract;

namespace mROA.Implementation.Backend
{
    public class NetworkGatewayModule : IGatewayModule
    {
        private readonly TcpListener _tcpListener;
        private readonly IConnectionHub _hub;
        private readonly IContextualSerializationToolKit _serialization;
        private readonly Dictionary<int, CancellationTokenSource> _extractorsCTS = new();
        private ICallIndexProvider _callIndexProvider;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly IMessageDistributorFactory _distributorFactory;
        public NetworkGatewayModule(IOptions<GatewayOptions> options, IIdentityGenerator identityGenerator, IContextualSerializationToolKit serialization, ICallIndexProvider callIndexProvider, IConnectionHub hub, IMessageDistributorFactory distributorFactory)
        {
            _tcpListener = new(options.Value.Endpoint);
            _identityGenerator = identityGenerator;
            _serialization = serialization;
            _callIndexProvider = callIndexProvider;
            _hub = hub;
            _distributorFactory = distributorFactory;
        }

        public void Run()
        {
            _tcpListener.Start();
            Console.WriteLine($"Listening on {_tcpListener.LocalEndpoint}");
            Console.WriteLine("Enter Backspace to stop");

            Task.Run(HandleIncomingConnections);
        }

        public void Dispose()
        {
            _tcpListener.Stop();
        }

        private async Task HandleIncomingConnections()
        {
            while (true)
            {
                var client = await _tcpListener.AcceptTcpClientAsync();
                Console.WriteLine($"Client connected from {client.Client.RemoteEndPoint}");
                var interaction = new ChannelInteractionModule(_serialization, _identityGenerator);
                
                var context = new EndPointContext(null, null);
                context.CallIndexProvider = _callIndexProvider;
                var streamExtractor =
                    new ChannelInteractionModule.StreamExtractor(client.GetStream(), _serialization, context);
                interaction.IsConnected = () => streamExtractor.IsConnected;
                streamExtractor.MessageReceived = async message =>
                {
                    await interaction.ReceiveChanel.Writer.WriteAsync(message);
                };
                _ = Task.Run(() => streamExtractor.SingleReceive());
                var connectionRequest = await interaction.ReceiveChanel.Reader.ReadAsync();
                var cts = new CancellationTokenSource();

                switch (connectionRequest.MessageType)
                {
                    case EMessageType.ClientConnect:
                        context.HostId = 0;
                        context.OwnerId = -interaction.ConnectionId;
                        interaction.Context = context;
                        Task.Run(async () => await streamExtractor.LoopedReceive(cts.Token));
                        _ = streamExtractor.SendFromChannel(interaction.TrustedPostChanel, cts.Token);
                        interaction.PostMessageAsync(new NetworkMessageHeader(_serialization!,
                            new IdAssignment { Id = interaction.ConnectionId }, null));
                        _extractorsCTS[interaction.ConnectionId] = cts;
                        _hub.RegisterInteraction(interaction);
                        Console.WriteLine("Client registered");
                        break;
                    case EMessageType.ClientRecovery:
                    {
                        var recoveryRequest = _serialization!.Deserialize<ClientRecovery>(connectionRequest.Data, null);
                        var recoveryInteraction = _hub.GetInteraction(recoveryRequest.Id);

                        _extractorsCTS[-recoveryRequest.Id].Cancel();

                        recoveryInteraction.IsConnected = () => streamExtractor.IsConnected;
                        streamExtractor.MessageReceived = message =>
                        {
                            recoveryInteraction.ReceiveChanel.Writer.WriteAsync(message);
                        };
                        _ = streamExtractor.SendFromChannel(recoveryInteraction.TrustedPostChanel, cts.Token);

                        Task.Run(async () => await streamExtractor.LoopedReceive(cts.Token));


                        recoveryInteraction.Restart(false);
                        break;
                    }
                    default:
                        client.Close();
                        break;
                }
            }
        }
    }

    public class GatewayOptions
    {
        public IPEndPoint Endpoint { get; set; }
        public Type InteractionModuleType { get; set; }
    }
}
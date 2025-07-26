using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using mROA.Abstract;

namespace mROA.Implementation.Backend
{
    public class NetworkGatewayModule : IGatewayModule
    {
        private readonly TcpListener _tcpListener;
        private readonly IConnectionHub _hub;
        private readonly HubRequestExtractor _hre;
        private readonly ILogger _logger;
        private readonly DistributionOptions _distribution;
        private readonly IContextualSerializationToolKit _serialization;
        private readonly Dictionary<int, CancellationTokenSource> _extractorsTokenSources = new();
        private readonly ICallIndexProvider _callIndexProvider;
        private readonly IIdentityGenerator _identityGenerator;

        public NetworkGatewayModule(IOptions<GatewayOptions> options, IIdentityGenerator identityGenerator,
            IContextualSerializationToolKit serialization, ICallIndexProvider callIndexProvider, IConnectionHub hub,
            IOptions<DistributionOptions> distribution, HubRequestExtractor hre,
            ILogger<ChannelInteractionModule.StreamExtractor> logger)
        {
            _tcpListener = new(options.Value.Endpoint);
            _identityGenerator = identityGenerator;
            _serialization = serialization;
            _callIndexProvider = callIndexProvider;
            _hub = hub;
            _hre = hre;
            _logger = logger;
            _distribution = distribution.Value;
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
                _ = HandleConnection(client).ConfigureAwait(false);
            }
        }

        private async Task HandleConnection(TcpClient client)
        {
            Console.WriteLine($"Client connected from {client.Client.RemoteEndPoint}");
            var interaction = new ChannelInteractionModule(_serialization, _identityGenerator);

            var context = new EndPointContext(null, null)
            {
                CallIndexProvider = _callIndexProvider
            };
            var streamExtractor =
                new ChannelInteractionModule.StreamExtractor(client.GetStream(), _serialization, context);
            interaction.IsConnected = () => streamExtractor.IsConnected;
            streamExtractor.MessageReceived = async message =>
            {
                await interaction.ReceiveChanel.Writer.WriteAsync(message).ConfigureAwait(false);
            };
            _ = Task.Run(() => streamExtractor.SingleReceive());
            var connectionRequest = await interaction.ReceiveChanel.Reader.ReadAsync();
            var cts = new CancellationTokenSource();

            switch (connectionRequest.MessageType)
            {
                case EMessageType.ClientConnect:
                    
                    HandleNewClient(context, interaction, streamExtractor, cts, connectionRequest);
                    break;
                case EMessageType.ClientRecovery:
                {
                    RecoverDisconnectedClient(connectionRequest, streamExtractor, cts);
                    break;
                }
                default:
                    client.Close();
                    break;
            }
        }

        private void HandleNewClient(EndPointContext context, ChannelInteractionModule interaction,
            ChannelInteractionModule.StreamExtractor streamExtractor, CancellationTokenSource cts, NetworkMessage connection)
        {
            context.HostId = 0;
            context.OwnerId = -interaction.ConnectionId;
            interaction.Context = context;
            Task.Run(async () => await streamExtractor.LoopedReceive(cts.Token));
            _ = streamExtractor.SendFromChannel(interaction.TrustedPostChanel, cts.Token);
            interaction.PostMessageAsync(new NetworkMessage(_serialization,
                new IdAssignment { Id = interaction.ConnectionId }, null));
            _extractorsTokenSources[interaction.ConnectionId] = cts;

            _hub.RegisterInteraction(interaction);
            var requestExtractor = _hre.HubOnOnConnected(new RepresentationModule(interaction, _serialization));

            if (_distribution.DistributionType != EDistributionType.Channeled)
            {
                BindRequestFirstDistribution(context, interaction, streamExtractor, requestExtractor);
            }
        }

        private void BindRequestFirstDistribution(IEndPointContext context, IChannelInteractionModule interaction,
            ChannelInteractionModule.StreamExtractor streamExtractor, IRequestExtractor requestExtractor)
        {
            var converters = requestExtractor.Converters;
            streamExtractor.MessageReceived = message =>
            {
                if (requestExtractor.Rule(message))
                {
                    for (var i = 0; i < converters.Length; i++)
                    {
                        var func = converters[i];
                        if (func(message) is not { } t) continue;
                        
                        var deserialized = _serialization.Deserialize(message.Data, t, context);
                        Task.Run(() => requestExtractor.PushMessage(deserialized, message.MessageType));
                        break;
                    }

                    return;
                }

                interaction.ReceiveChanel.Writer.WriteAsync(message).ConfigureAwait(false);
            };
        }

        private void RecoverDisconnectedClient(NetworkMessage connectionRequest,
            ChannelInteractionModule.StreamExtractor streamExtractor,
            CancellationTokenSource cts)
        {
            var recoveryRequest = _serialization.Deserialize<ClientRecovery>(connectionRequest.Data, null);
            var recoveryInteraction = _hub.GetInteraction(recoveryRequest.Id);

            _extractorsTokenSources[-recoveryRequest.Id].Cancel();

            recoveryInteraction.IsConnected = () => streamExtractor.IsConnected;
            streamExtractor.MessageReceived = message =>
            {
                recoveryInteraction.ReceiveChanel.Writer.WriteAsync(message).ConfigureAwait(false);
            };
            _ = streamExtractor.SendFromChannel(recoveryInteraction.TrustedPostChanel, cts.Token);

            if (_distribution.DistributionType == EDistributionType.ExtractorFirst)
            {
                BindRequestFirstDistribution(recoveryInteraction.Context, recoveryInteraction, streamExtractor,
                    _hre[recoveryInteraction.ConnectionId]);
            }

            Task.Run(async () => await streamExtractor.LoopedReceive(cts.Token).ConfigureAwait(false));

            recoveryInteraction.Restart(false);
        }
    }

    public class GatewayOptions
    {
        public IPEndPoint Endpoint { get; set; }
    }
}
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using mROA.Abstract;
using mROA.Implementation.Backend;
using Exception = System.Exception;

namespace mROA.Implementation.Frontend
{
    public class NetworkFrontendBridge : IFrontendBridge
    {
        private readonly IPEndPoint _serverEndPoint;
        private TcpClient _tcpClient = new();
        private readonly IChannelInteractionModule _interactionModule;
        private readonly IContextualSerializationToolKit _serialization;
        private ChannelInteractionModule.StreamExtractor _currentExtractor;
        private CancellationTokenSource _rawExtractorCancellation;
        private readonly IEndPointContext _context;

        public NetworkFrontendBridge(IOptions<GatewayOptions> options, IEndPointContext context,
            IContextualSerializationToolKit serialization, IChannelInteractionModule interactionModule)
        {
            _serverEndPoint = options.Value.Endpoint;
            _context = context;
            _serialization = serialization;
            _interactionModule = interactionModule;
            _rawExtractorCancellation = new CancellationTokenSource();
            _currentExtractor = new ChannelInteractionModule.StreamExtractor(Stream.Null);
        }

        public async Task Connect()
        {
            _tcpClient.Connect(_serverEndPoint);
            _tcpClient.NoDelay = true;
            PrepareExtractor();
            _interactionModule.IsConnected = () => _currentExtractor.IsConnected;
            _interactionModule.OnDisconnected += _ => { Reconnect().ConfigureAwait(false); };

            _interactionModule.PostMessageAsync(new NetworkMessage(_serialization, new ClientConnect(), _context))
                .Wait();

            _ = _currentExtractor.SingleReceive().ConfigureAwait(false);
            var idMessage = await _interactionModule.GetNextMessageReceiving();

            if (idMessage.MessageType != EMessageType.IdAssigning)
            {
                throw new Exception(
                    $"Incorrect message type. Must be IdAssigning, current : {idMessage.MessageType.ToString()}");
            }


            _ = Task.Run(async () => await _currentExtractor.LoopedReceive(_rawExtractorCancellation.Token));

            var assignment = _serialization.Deserialize<IdAssignment>(idMessage.Data, _context);
            _interactionModule.ConnectionId = -assignment.Id;
            _context.HostId = assignment.Id;
            _context.OwnerId = assignment.Id;
        }

        private void PrepareExtractor()
        {
            _currentExtractor =
                new ChannelInteractionModule.StreamExtractor(_tcpClient.GetStream());

            _ = _currentExtractor.SendFromChannel(_interactionModule.TrustedPostChanel,
                _rawExtractorCancellation.Token);
            _currentExtractor.MessageReceived = message =>
            {
                _interactionModule.ReceiveChanel.Writer.WriteAsync(message);
            };
        }

        private async Task Reconnect()
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(_serverEndPoint);

            _rawExtractorCancellation.Cancel();
            _rawExtractorCancellation = new CancellationTokenSource();

            PrepareExtractor();

            _ = Task.Run(async () => await _currentExtractor.LoopedReceive(_rawExtractorCancellation.Token));

            await _interactionModule.Restart(true);
        }

        public void Obstacle()
        {
            _tcpClient.Dispose();
        }

        public void Disconnect()
        {
            _ = _interactionModule.PostMessageAsync(new NetworkMessage(_serialization, new ClientDisconnect(),
                _context));
            _interactionModule.Dispose();
            _tcpClient.Dispose();
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
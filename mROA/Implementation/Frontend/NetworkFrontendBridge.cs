using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using mROA.Abstract;
using Exception = System.Exception;

namespace mROA.Implementation.Frontend
{
    public class NetworkFrontendBridge : IFrontendBridge
    {
        private readonly IPEndPoint _serverEndPoint;
        private TcpClient _tcpClient = new();
        private IChannelInteractionModule? _interactionModule;
        private IContextualSerializationToolKit? _serialization;
        private ChannelInteractionModule.StreamExtractor _currentExtractor;
        private CancellationTokenSource _rawExtractorCancellation;
        private IEndPointContext _context;

        public NetworkFrontendBridge(IPEndPoint serverEndPoint)
        {
            _serverEndPoint = serverEndPoint;
            _rawExtractorCancellation = new CancellationTokenSource();
        }

        public void Inject<T>(T dependency)
        {
            switch (dependency)
            {
                case ChannelInteractionModule interactionModule:
                    _interactionModule = interactionModule;
                    break;
                case IContextualSerializationToolKit toolkit:
                    _serialization = toolkit;
                    break;
                case IEndPointContext endPointContext:
                    _context = endPointContext;
                    break;
            }
        }

        public async Task Connect()
        {
            if (_interactionModule is null)
                throw new Exception("Interaction module was not injected");
            if (_serialization == null)
                throw new NullReferenceException("Serialization toolkit is not initialized");

            _tcpClient.Connect(_serverEndPoint);

            PrepareExtractor();
            _interactionModule.IsConnected = () => _currentExtractor.IsConnected;
            _interactionModule.OnDisconnected += _ => { Reconnect(); };

            _interactionModule.PostMessageAsync(new NetworkMessageHeader(_serialization, new ClientConnect(), _context))
                .Wait();

            _currentExtractor.SingleReceive();
            var idMessage = await _interactionModule.GetNextMessageReceiving(false);

            if (idMessage.MessageType != EMessageType.IdAssigning)
            {
                throw new Exception(
                    $"Incorrect message type. Must be IdAssigning, current : {idMessage.MessageType.ToString()}");
            }


            Task.Run(async () => await _currentExtractor.LoopedReceive(_rawExtractorCancellation.Token));

            var assignment = _serialization.Deserialize<IdAssignment>(idMessage.Data, _context);
            _interactionModule.ConnectionId = -assignment.Id;
            _context.HostId = assignment.Id;
            _context.OwnerId = assignment.Id;
        }

        private void PrepareExtractor()
        {
            _currentExtractor =
                new ChannelInteractionModule.StreamExtractor(_tcpClient.GetStream(), _serialization!, _context);

            _ = _currentExtractor.SendFromChannel(_interactionModule!.TrustedPostChanel,
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

            Task.Run(async () => await _currentExtractor.LoopedReceive(_rawExtractorCancellation.Token));

            await _interactionModule.Restart(true);
        }

        public void Obstacle()
        {
            _tcpClient.Dispose();
        }

        public void Disconnect()
        {
            _ = _interactionModule!.PostMessageAsync(new NetworkMessageHeader(_serialization!, new ClientDisconnect(),
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
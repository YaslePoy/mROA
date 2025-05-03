using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
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
        private ISerializationToolkit? _serialization;
        private ChannelInteractionModule.StreamExtractor _currentExtractor;
        private CancellationTokenSource _rawExtractorCancellation;

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
                case ISerializationToolkit toolkit:
                    _serialization = toolkit;
                    break;
            }
        }

        public void Connect()
        {
            if (_interactionModule is null)
                throw new Exception("Interaction module was not injected");
            if (_serialization == null)
                throw new NullReferenceException("Serialization toolkit is not initialized");

            _tcpClient.Connect(_serverEndPoint);

            PrepareExtractor();
            _interactionModule.IsConnected = () => _currentExtractor.IsConnected;
            _interactionModule.OnDisconnected += _ => { Reconnect(); };

            _interactionModule.PostMessageAsync(new NetworkMessageHeader(_serialization, new ClientConnect())).Wait();

            _currentExtractor.SingleReceive();
            var idMessage = _interactionModule.GetNextMessageReceiving(false).GetAwaiter().GetResult();

            if (idMessage.MessageType != EMessageType.IdAssigning)
            {
                throw new Exception(
                    $"Incorrect message type. Must be IdAssigning, current : {idMessage.MessageType.ToString()}");
            }


            Task.Run(async () => await _currentExtractor.LoopedReceive(_rawExtractorCancellation.Token));

            var assignment = _serialization.Deserialize<IdAssignment>(idMessage.Data)!;
            _interactionModule.ConnectionId = -assignment.Id;
            TransmissionConfig.OwnershipRepository = new StaticOwnershipRepository(assignment.Id);
        }

        private void PrepareExtractor()
        {
            _currentExtractor = new ChannelInteractionModule.StreamExtractor(_tcpClient.GetStream(), _serialization!);

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
            _ = _interactionModule!.PostMessageAsync(new NetworkMessageHeader(_serialization!, new ClientDisconnect()));
            _interactionModule.Dispose();
            _tcpClient.Dispose();
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
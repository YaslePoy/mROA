using System;
using System.Net;
using System.Net.Sockets;
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
        private INextGenerationInteractionModule? _interactionModule;
        private ISerializationToolkit? _serialization;

        public NetworkFrontendBridge(IPEndPoint serverEndPoint)
        {
            _serverEndPoint = serverEndPoint;
        }

        public void Inject<T>(T dependency)
        {
            switch (dependency)
            {
                case NextGenerationInteractionModule interactionModule:
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

            _interactionModule.BaseStream = _tcpClient.GetStream();
            _interactionModule.UntrustedReceiveChanel = Channel.CreateUnbounded<NetworkMessageHeader>(new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = false,
                AllowSynchronousContinuations = true
            }).Reader;
            _interactionModule.OnDisconnected += id => { Reconnect(); };

            _interactionModule.PostMessageAsync(new NetworkMessageHeader(_serialization, new ClientConnect())).Wait();
            var idMessage = _interactionModule.GetNextMessageReceiving(false).GetAwaiter().GetResult();
            if (idMessage.MessageType != EMessageType.IdAssigning)
            {
                throw new Exception(
                    $"Incorrect message type. Must be IdAssigning, current : {idMessage.MessageType.ToString()}");
            }


            var assignment = _serialization.Deserialize<IdAssignment>(idMessage.Data)!;
            _interactionModule.ConnectionId = -assignment.Id;
            TransmissionConfig.OwnershipRepository = new StaticOwnershipRepository(assignment.Id);
        }

        private async Task Reconnect()
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(_serverEndPoint);
            _interactionModule.BaseStream = _tcpClient.GetStream();
            _interactionModule.UntrustedReceiveChanel = Channel.CreateUnbounded<NetworkMessageHeader>(new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = false,
                AllowSynchronousContinuations = true
            }).Reader;
            await _interactionModule.Restart(true);
        }

        public void Obstacle()
        {
            _interactionModule!.BaseStream!.Dispose();
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
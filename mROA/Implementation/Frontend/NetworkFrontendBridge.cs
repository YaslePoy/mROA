using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using mROA.Abstract;
using Exception = System.Exception;

namespace mROA.Implementation.Frontend
{
    public class NetworkFrontendBridge : IFrontendBridge
    {
        private readonly IPEndPoint _ipEndPoint;
        private readonly TcpClient _tcpClient = new();
        private NextGenerationInteractionModule? _interactionModule;
        private ISerializationToolkit? _serialization;

        public NetworkFrontendBridge(IPEndPoint ipEndPoint)
        {
            _ipEndPoint = ipEndPoint;
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

            _tcpClient.Connect(_ipEndPoint);

            _interactionModule.BaseStream = _tcpClient.GetStream();

            _interactionModule.OnDisconected += async id =>
            {
                await Reconect();
            };
            _ = _interactionModule.PostMessageAsync(new NetworkMessageHeader(_serialization, new ClientConnect()));
            var welcomeMessage = _interactionModule.GetNextMessageReceiving().GetAwaiter().GetResult();
            if (welcomeMessage.MessageType != EMessageType.IdAssigning)
            {
                throw new Exception(
                    $"Incorrect message type. Must be IdAssigning, current : {welcomeMessage.MessageType.ToString()}");
            }


            var assignment = _serialization.Deserialize<IdAssignment>(welcomeMessage.Data)!;
            _interactionModule.ConnectionId = -assignment.Id;
            TransmissionConfig.OwnershipRepository = new StaticOwnershipRepository(assignment.Id);
        }

        private async Task Reconect()
        {
            _tcpClient.Connect(_ipEndPoint);
            _interactionModule.BaseStream = _tcpClient.GetStream();
            await _interactionModule.Restart();
        }

        public void Obstacle()
        {
            _interactionModule!.BaseStream!.Close();
            _tcpClient.Close();
        }

        public void Disconnect()
        {
            _ = _interactionModule!.PostMessageAsync(new NetworkMessageHeader(_serialization!, new ClientDisconnect()));
            _interactionModule.BaseStream!.Close();
            _tcpClient.Dispose();
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
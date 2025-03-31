using System;
using System.Net;
using System.Net.Sockets;
using mROA.Abstract;

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
            var welcomeMessage = _interactionModule.GetNextMessageReceiving().GetAwaiter().GetResult();
            if (welcomeMessage.EMessageType != EMessageType.IdAssigning)
            {
                throw new Exception(
                    $"Incorrect message type. Must be IdAssigning, current : {welcomeMessage.EMessageType.ToString()}");
            }


            var assignment = _serialization.Deserialize<IdAssignment>(welcomeMessage.Data)!;
            _interactionModule.ConnectionId = -assignment.Id;
            TransmissionConfig.OwnershipRepository = new StaticOwnershipRepository(assignment.Id);
        }
    }
}
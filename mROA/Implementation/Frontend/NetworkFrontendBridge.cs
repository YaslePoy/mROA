using System;
using System.Net;
using System.Net.Sockets;
using mROA.Abstract;

namespace mROA.Implementation.Frontend
{
    public class NetworkFrontendBridge : IFrontendBridge
    {
        private readonly TcpClient _tcpClient = new();
        private NextGenerationInteractionModule? _interactionModule;
        private ISerializationToolkit? _serialization;
        private readonly IPEndPoint _ipEndPoint;

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
            if (welcomeMessage.SchemaId != MessageType.IdAssigning)
            {
                throw new Exception($"Incorrect message type. Must be IdAssigning, current : {welcomeMessage.SchemaId.ToString()}");
            }

            TransmissionConfig.OwnershipRepository = new StaticOwnershipRepository(_serialization.Deserialize<IdAssingnment>(welcomeMessage.Data)!.Id);
        }
    }
}
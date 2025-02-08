using System.Net;
using System.Net.Sockets;
using global::System;
using mROA.Abstract;

namespace mROA.Implementation.Frontend
{
    public class NetworkFrontendBridge : IFrontendBridge
    {
        private readonly TcpClient _tcpClient = new();
        private StreamBasedFrontendInteractionModule? _interactionModule;
        private readonly IPEndPoint _ipEndPoint;

        public NetworkFrontendBridge(IPEndPoint ipEndPoint)
        {
            _ipEndPoint = ipEndPoint;
        }

        public void Inject<T>(T dependency)
        {
            if (dependency is StreamBasedFrontendInteractionModule interactionModule)
            {
                _interactionModule = interactionModule;
            }
        }

        public void Connect()
        {
            if (_interactionModule is null)
                throw new Exception("Interaction module was not injected");
        
            _tcpClient.Connect(_ipEndPoint);
            _interactionModule.ServerStream = _tcpClient.GetStream();
        }
    }
}
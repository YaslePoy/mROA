using System.Net;
using System.Net.Sockets;
using global::System;
using global::System.Threading.Tasks;
using mROA.Abstract;

namespace mROA.Implementation.Backend
{
    public class NetworkGatewayModule : IGatewayModule
    {
        private readonly TcpListener _tcpListener;
        private IInteractionModule? _interactionModule;

        public NetworkGatewayModule(IPEndPoint endpoint)
        {
            _tcpListener = new TcpListener(endpoint);
        }


        public void Run()
        {
            _tcpListener.Start();
        
            Task.Run(HandleIncomingConnections);

            while (true)
            {
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.Backspace)
                    break;
            }

        
        
        }

        public void Dispose()
        {
            _tcpListener.Stop();
        }

        private void HandleIncomingConnections()
        {
            if (_interactionModule is null)
                throw new NullReferenceException("Interaction module is null");
        
            while (true)
            {
                var client = _tcpListener.AcceptTcpClient();
                _interactionModule.RegisterSourse(client.GetStream());
            }
        }

        public void Inject<T>(T dependency)
        {
            if (dependency is IInteractionModule interactionModule)
                _interactionModule = interactionModule;
        }

    }
}
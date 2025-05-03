using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class UdpUntrustedInteraction : IUntrustedInteractionModule
    {
        private ISerializationToolkit _serializationToolkit;
        private IChannelInteractionModule _channelInteractionModule;
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();
        public void Dispose()
        {
            _tokenSource.Cancel();
        }

        public Task Start(IPEndPoint endpoint)
        {
            return Task.Run(() =>
            {
                var client = new UdpClient();
                client.Connect(endpoint);
                Listening(client, _tokenSource.Token);
                Posting(client, _tokenSource.Token);
            }, _tokenSource.Token);
        }

        private async Task Listening(UdpClient udpClient, CancellationToken token)
        {
            var writer = _channelInteractionModule.ReceiveChanel.Writer;
            while (token.IsCancellationRequested == false)
            {
                var message = new Memory<byte>((await udpClient.ReceiveAsync()).Buffer);
                var parsed = _serializationToolkit.Deserialize<NetworkMessageHeader>(message.Span)!;
                await writer.WriteAsync(parsed, token);
            }
        }

        private async Task Posting(UdpClient udpClient, CancellationToken token)
        {
            await foreach (var post in _channelInteractionModule.UntrustedPostChanel.ReadAllAsync(token))
            {
                var serialized = _serializationToolkit.Serialize(post);
                await udpClient.SendAsync(serialized, serialized.Length);
            }
        }
        
        public void Inject<T>(T dependency)
        {
            switch (dependency)
            {
                case IChannelInteractionModule channelModule:
                    _channelInteractionModule = channelModule;
                    break;
                case ISerializationToolkit serializationToolkit:
                    _serializationToolkit = serializationToolkit;
                    break;
            }
        }
    }
}
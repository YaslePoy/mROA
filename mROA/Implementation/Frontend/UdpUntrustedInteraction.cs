using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using mROA.Abstract;

namespace mROA.Implementation.Frontend
{
    public class UdpUntrustedInteraction : IUntrustedInteractionModule
    {
        private IContextualSerializationToolKit _serializationToolkit;
        private IChannelInteractionModule _channelInteractionModule;
        private CancellationTokenSource _tokenSource = new();

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
            var initMessage = new NetworkMessageHeader
            {
                MessageType = EMessageType.UntrustedConnect, Id = Guid.NewGuid(),
                Data = BitConverter.GetBytes(Math.Abs(_channelInteractionModule.ConnectionId))
            };

            var initParsed = _serializationToolkit.Serialize(initMessage);

            await udpClient.SendAsync(initParsed, initParsed.Length);

            await foreach (var post in _channelInteractionModule.UntrustedPostChanel.ReadAllAsync(token))
            {
                if (post.MessageType is not (EMessageType.CallRequest or EMessageType.CancelRequest
                    or EMessageType.EventRequest))
                    continue;

                var serialized = _serializationToolkit.Serialize(post);
#if TRACE
                Console.WriteLine("Untrusted write start");
#endif
                await udpClient.SendAsync(serialized, serialized.Length);
#if TRACE
                Console.WriteLine("Untrusted write finished");
#endif
            }
        }

        public void Inject<T>(T dependency)
        {
            switch (dependency)
            {
                case IChannelInteractionModule channelModule:
                    _channelInteractionModule = channelModule;
                    break;
                case IContextualSerializationToolKit serializationToolkit:
                    _serializationToolkit = serializationToolkit;
                    break;
            }
        }
    }
}
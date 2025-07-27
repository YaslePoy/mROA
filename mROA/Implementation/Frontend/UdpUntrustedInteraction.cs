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
        private readonly IContextualSerializationToolKit _serializationToolkit;
        private readonly IChannelInteractionModule _channelInteractionModule;
        private readonly CancellationTokenSource _tokenSource = new();
        private readonly IEndPointContext _context;

        public UdpUntrustedInteraction(IContextualSerializationToolKit serializationToolkit,
            IChannelInteractionModule channelInteractionModule, IEndPointContext context)
        {
            _serializationToolkit = serializationToolkit;
            _channelInteractionModule = channelInteractionModule;
            _context = context;
        }

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
                var parsed = _serializationToolkit.Deserialize<NetworkMessage>(message, _context);

                await writer.WriteAsync(parsed, token);
            }
        }

        private async Task Posting(UdpClient udpClient, CancellationToken token)
        {
            var initMessage = new NetworkMessage
            {
                MessageType = EMessageType.UntrustedConnect, Id = RequestId.Generate(),
                Data = BitConverter.GetBytes(_channelInteractionModule.ConnectionId)
            };

            var initParsed = _serializationToolkit.Serialize(initMessage, _context);

            await udpClient.SendAsync(initParsed, initParsed.Length);

            await foreach (var post in _channelInteractionModule.UntrustedPostChanel.ReadAllAsync(token))
            {
                if (post.MessageType is not (EMessageType.CallRequest or EMessageType.CancelRequest
                    or EMessageType.EventRequest))
                    continue;

                var serialized = _serializationToolkit.Serialize(post, _context);

                await udpClient.SendAsync(serialized, serialized.Length);
            }
        }
    }
}
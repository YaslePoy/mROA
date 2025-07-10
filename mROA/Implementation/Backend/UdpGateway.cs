using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using mROA.Abstract;
using static mROA.Implementation.EMessageType;

namespace mROA.Implementation.Backend
{
    public class UdpGateway : IUntrustedGateway
    {
        private IConnectionHub _hub;
        private UdpClient _client;
        private Dictionary<IPEndPoint, int> _reservedPorts = new();
        private CancellationTokenSource _tokenSource = new();
        private IContextualSerializationToolKit _serializationToolkit;
        private IEndPointContext _context;

        public UdpGateway(IPEndPoint listeningEndpoint)
        {
            _client = new UdpClient(listeningEndpoint);
        }


        public void Inject(object dependency)
        {
            switch (dependency)
            {
                case IConnectionHub hub:
                    _hub = hub;
                    break;
                case IContextualSerializationToolKit serializationToolkit:
                    _serializationToolkit = serializationToolkit;
                    break;
                case IEndPointContext context:
                    _context = context;
                    break;
            }
        }

        public void Dispose()
        {
            _tokenSource.Cancel();
            _client.Close();
        }

        public Task Start()
        {
            var token = _tokenSource.Token;
            return Task.Run(async () =>
            {
                while (token.IsCancellationRequested == false)
                {
                    var incoming = await _client.ReceiveAsync();
                    var parsed = _serializationToolkit.Deserialize<NetworkMessageHeader>(incoming.Buffer, _context);
                    try
                    {
                        int channelId;
                        switch (parsed.MessageType)
                        {
                            case UntrustedConnect:
                                channelId = BitConverter.ToInt32(parsed.Data);
                                _reservedPorts[incoming.RemoteEndPoint] = channelId;
                                _ = UntrustedSend(_hub.GetInteraction(channelId), incoming.RemoteEndPoint);
                                break;
                            default:
                                channelId = _reservedPorts[incoming.RemoteEndPoint];
                                var interaction = _hub.GetInteraction(channelId);
                                await interaction.ReceiveChanel.Writer.WriteAsync(parsed, token);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }, token);
        }

        private Task UntrustedSend(IChannelInteractionModule interaction, IPEndPoint endpoint)
        {
            return Task.Run(async () =>
                {
                    await foreach (var post in interaction.UntrustedPostChanel.ReadAllAsync())
                    {
                        if (post.MessageType is not (CallRequest or EMessageType.CancelRequest
                            or EventRequest))
                            continue;

                        var parsed = _serializationToolkit.Serialize(post, _context);
                        await _client.SendAsync(parsed, parsed.Length, endpoint);
                    }
                }
            );
        }
    }
}
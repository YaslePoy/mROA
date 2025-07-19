using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using mROA.Abstract;
using static mROA.Implementation.EMessageType;

namespace mROA.Implementation.Backend
{
    public class UdpGateway : IUntrustedGateway
    {
        private readonly IConnectionHub _hub;
        private readonly UdpClient _client;
        private readonly Dictionary<IPEndPoint, int> _reservedPorts = new();
        private readonly CancellationTokenSource _tokenSource = new();
        private readonly IContextualSerializationToolKit _serializationToolkit;

        public UdpGateway(IOptions<GatewayOptions> options, IConnectionHub hub, IContextualSerializationToolKit serializationToolkit)
        {
            _hub = hub;
            _serializationToolkit = serializationToolkit;
            _client = new UdpClient(options.Value.Endpoint);
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
                    var parsed = _serializationToolkit.Deserialize<NetworkMessageHeader>(incoming.Buffer, null);
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

                        var parsed = _serializationToolkit.Serialize(post, null);
                        await _client.SendAsync(parsed, parsed.Length, endpoint);
                    }
                }
            );
        }
    }
}
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using mROA.Abstract;

namespace mROA.Implementation.Backend
{
    public class UdpGateway : IUntrustedGateway
    {
        private IConnectionHub _hub;
        private UdpClient _client;
        private Dictionary<IPEndPoint, int> _reservedPorts = new();
        private CancellationTokenSource _tokenSource = new();
        private ISerializationToolkit _serializationToolkit;

        public UdpGateway(IPEndPoint listeningEndpoint)
        {
            _client = new UdpClient(listeningEndpoint);
        }


        public void Inject<T>(T dependency)
        {
            switch (dependency)
            {
                case IConnectionHub hub:
                    _hub = hub;
                    break;
                case ISerializationToolkit serializationToolkit:
                    _serializationToolkit = serializationToolkit;
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
                    var parsed = _serializationToolkit.Deserialize<NetworkMessageHeader>(incoming.Buffer);
                    try
                    {
                        int channelId;
                        switch (parsed.MessageType)
                        {
                            case EMessageType.UntrustedConnect:
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
                        var parsed = _serializationToolkit.Serialize(post);
                        await _client.SendAsync(parsed, parsed.Length, endpoint);
                    }
                }
            );
        }
    }
}
using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IChannelInteractionModule : IDisposable
    {
        int ConnectionId { get; set; }
        IEndPointContext Context { get; set; }
        Channel<NetworkMessage> ReceiveChanel { get; }
        ChannelReader<NetworkMessage> TrustedPostChanel { get; }
        ChannelReader<NetworkMessage> UntrustedPostChanel { get; }
        Func<bool> IsConnected { get; set; }
        ValueTask<NetworkMessage> GetNextMessageReceiving();
        Task PostMessageAsync(NetworkMessage message);
        Task PostMessageUntrustedAsync(NetworkMessage message);
        event Action<int> OnDisconnected;
        Task Restart(bool sendRecovery);
        void PassReconnection();
    }
}
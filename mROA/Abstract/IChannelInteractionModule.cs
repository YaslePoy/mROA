using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IChannelInteractionModule : IInjectableModule, IDisposable
    {
        int ConnectionId { get; set; }
        Channel<NetworkMessageHeader> ReceiveChanel { get; }
        ChannelReader<NetworkMessageHeader> TrustedPostChanel { get; }
        ChannelReader<NetworkMessageHeader> UntrustedPostChanel { get; }
        Func<bool> IsConnected { get; set; }
        ValueTask<NetworkMessageHeader> GetNextMessageReceiving(bool infinite = true);
        Task PostMessageAsync(NetworkMessageHeader messageHeader);
        Task PostMessageUntrustedAsync(NetworkMessageHeader messageHeader);
        event Action<int> OnDisconnected;
        Task Restart(bool sendRecovery);
    }
}
using System;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IChannelInteractionModule : IInjectableModule, IDisposable
    {
        int ConnectionId { get; set; }
        ChannelWriter<NetworkMessageHeader> ReceiveChanel { get; }
        ChannelReader<NetworkMessageHeader> TrustedPostChanel { get; }
        ChannelReader<NetworkMessageHeader> UntrustedPostChanel { get; }
        Func<bool> IsConnected { get; set; }
        Task<NetworkMessageHeader> GetNextMessageReceiving(bool infinite = true);
        Task PostMessageAsync(NetworkMessageHeader messageHeader);
        Task PostMessageUntrustedAsync(NetworkMessageHeader messageHeader);
        void HandleMessage(NetworkMessageHeader messageHeader);
        NetworkMessageHeader? FirstByFilter(Predicate<NetworkMessageHeader> predicate);
        event Action<int> OnDisconnected;
        Task Restart(bool sendRecovery);
    }
}
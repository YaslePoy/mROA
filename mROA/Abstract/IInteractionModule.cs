using System;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface INextGenerationInteractionModule : IInjectableModule, IDisposable
    {
        int ConnectionId { get; set; }
        Stream? BaseStream { get; set; }
        ChannelReader<NetworkMessageHeader> UntrustedReceiveChanel { get; set; }
        ChannelWriter<(int clientId, NetworkMessageHeader messageHeader)> UntrustedPostChanel { get; set; }
        
        Task<NetworkMessageHeader> GetNextMessageReceiving(bool infinite = true);
        Task PostMessageAsync(NetworkMessageHeader messageHeader);
        Task PostMessageUntrustedAsync(NetworkMessageHeader messageHeader);
        void HandleMessage(NetworkMessageHeader messageHeader);
        // NetworkMessageHeader[] UnhandledMessages { get; }
        NetworkMessageHeader? FirstByFilter(Predicate<NetworkMessageHeader> predicate);
        event Action<int> OnDisconnected;
        Task Restart(bool sendRecovery);
    }
}
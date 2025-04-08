using System;
using System.IO;
using System.Threading.Tasks;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface INextGenerationInteractionModule : IInjectableModule
    {
        int ConnectionId { get; set; }
        public Stream? BaseStream { get; set; }
        Task<NetworkMessageHeader> GetNextMessageReceiving();
        Task PostMessageAsync(NetworkMessageHeader messageHeader);
        void HandleMessage(NetworkMessageHeader messageHeader);
        NetworkMessageHeader[] UnhandledMessages { get; }
        NetworkMessageHeader? FirstByFilter(Predicate<NetworkMessageHeader> predicate);
        event Action<int> OnDisconected;
        Task Restart(bool sendRecovery);

    }
}
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
        Task PostMessage(NetworkMessageHeader messageHeader);
        void HandleMessage(NetworkMessageHeader messageHeader);
        NetworkMessageHeader[] UnhandledMessages { get; }
        NetworkMessageHeader? FirstByFilter(Predicate<NetworkMessageHeader> predicate);
    }
}
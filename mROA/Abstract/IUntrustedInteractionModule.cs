using System;
using System.Net;
using System.Threading.Tasks;

namespace mROA.Abstract
{
    public interface IUntrustedInteractionModule : IInjectableModule, IDisposable
    {
        Task Start(IPEndPoint endpoint);
    }
}
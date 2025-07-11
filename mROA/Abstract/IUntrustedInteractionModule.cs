using System;
using System.Net;
using System.Threading.Tasks;

namespace mROA.Abstract
{
    public interface IUntrustedInteractionModule : IDisposable
    {
        Task Start(IPEndPoint endpoint);
    }
}
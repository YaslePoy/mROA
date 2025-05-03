using System;
using System.Net;
using System.Threading.Tasks;

namespace mROA.Abstract
{
    public interface IUntrustedGateway : IInjectableModule, IDisposable
    {
        Task Start();
    }
}
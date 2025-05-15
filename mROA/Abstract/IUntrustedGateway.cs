using System;
using System.Threading.Tasks;

namespace mROA.Abstract
{
    public interface IUntrustedGateway : IInjectableModule, IDisposable
    {
        Task Start();
    }
}
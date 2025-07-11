using System;
using System.Threading.Tasks;

namespace mROA.Abstract
{
    public interface IUntrustedGateway : IDisposable
    {
        Task Start();
    }
}
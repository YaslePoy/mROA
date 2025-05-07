using System;
using System.Threading.Tasks;

namespace mROA.Abstract
{
    public interface IFrontendBridge : IInjectableModule, IDisposable
    {
        Task Connect();
        void Obstacle();
        void Disconnect();
    }
}
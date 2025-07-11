using System;
using System.Threading.Tasks;

namespace mROA.Abstract
{
    public interface IFrontendBridge : IDisposable
    {
        Task Connect();
        void Obstacle();
        void Disconnect();
    }
}
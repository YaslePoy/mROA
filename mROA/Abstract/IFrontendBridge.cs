using System;

namespace mROA.Abstract
{
    public interface IFrontendBridge : IInjectableModule, IDisposable
    {
        void Connect();
        void Obstacle();
        void Disconnect();
    }
}
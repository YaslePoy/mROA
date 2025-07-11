using System;

namespace mROA.Abstract
{
    public interface IGatewayModule : IDisposable
    {
        void Run();
    }
}
using System;

namespace mROA.Abstract
{
    public interface IGatewayModule : IDisposable, IInjectableModule
    {
        void Run();
    }
}
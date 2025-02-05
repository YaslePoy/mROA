using mROA.Abstract;

namespace mROA.Implementation;

public interface IGatewayModule : IDisposable, IInjectableModule
{ 
    void Run();
}
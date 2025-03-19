using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IRemoteObjectFactory : IInjectableModule
    {
        T Produce<T>(ComplexObjectIdentifier id);
    }
}
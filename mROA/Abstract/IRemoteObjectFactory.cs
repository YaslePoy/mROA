using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IRemoteObjectFactory
    {
        T Produce<T>(ComplexObjectIdentifier id, IEndPointContext context);
    }
}
using System;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IInstanceRepository : IInjectableModule
    {
        int ResisterObject<T>(object o, IEndPointContext context);
        void ClearObject(ComplexObjectIdentifier id, IEndPointContext context);
        T GetObject<T>(ComplexObjectIdentifier id, IEndPointContext context) where T : class;
        T GetSingletonObject<T>(IEndPointContext context) where T : class, IShared;
        object GetSingletonObject(Type type, IEndPointContext context);
        int GetObjectIndex<T>(object o, IEndPointContext context);
    }
}
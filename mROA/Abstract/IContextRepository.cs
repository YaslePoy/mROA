using System;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IContextRepository : IInjectableModule
    {
        int HostId { get; set; }
        int ResisterObject<T>(object o, IEndPointContext context);
        void ClearObject(ComplexObjectIdentifier id);
        T GetObject<T>(ComplexObjectIdentifier id, IEndPointContext context);
        object GetSingleObject(Type type, int ownerId);
        int GetObjectIndex<T>(object o, IEndPointContext context);
    }
}
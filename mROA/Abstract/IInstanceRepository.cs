using System;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IInstanceRepository : IInjectableModule
    {
        int HostId { get; set; }
        int ResisterObject<T>(object o, IEndPointContext context);
        void ClearObject(ComplexObjectIdentifier id, IEndPointContext context);
        T GetObject<T>(ComplexObjectIdentifier id, IEndPointContext context);
        object GetSingleObject(Type type, IEndPointContext context);
        int GetObjectIndex<T>(object o, IEndPointContext context);
    }
}
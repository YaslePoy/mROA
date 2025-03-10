using System;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IContextRepository : IInjectableModule
    {
        int HostId { get; set; }
        int ResisterObject<T>(object o, IEndPointContext context);
        void ClearObject(ComplexObjectIdentifier id);
        T GetObjectByShell<T>(SharedObjectShellShell<T> sharedObjectShellShell);
        T? GetObject<T>(ComplexObjectIdentifier id);
        object GetSingleObject(Type type);
        int GetObjectIndex<T>(object o, IEndPointContext context);
    }
}
using System;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IContextRepository : IInjectableModule
    {
        int ResisterObject<T>(object o, IEndPointContext context);
        void ClearObject(int id);
        T GetObjectByShell<T>(SharedObjectShellShell<T> sharedObjectShellShell);
        T? GetObject<T>(int id);
        object GetSingleObject(Type type);
        int GetObjectIndex<T>(object o, IEndPointContext context);
    }
}
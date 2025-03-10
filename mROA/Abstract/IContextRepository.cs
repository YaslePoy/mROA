using System;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IContextRepository : IInjectableModule
    {
        int ResisterObject(object o, IEndPointContext context);
        void ClearObject(int id);
        T GetObjectBySharedObject<T>(SharedObjectShellShell<T> sharedObjectShellShell);
        T? GetObject<T>(int id);
        object GetSingleObject(Type type);
        int GetObjectIndex(object o, IEndPointContext context);
    }
}
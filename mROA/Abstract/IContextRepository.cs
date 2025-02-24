using System;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IContextRepository : IInjectableModule
    {
        int ResisterObject(object o);
        void ClearObject(int id);
        T GetObjectBySharedObject<T>(SharedObject<T> sharedObject);
        T? GetObject<T>(int id);
        object GetSingleObject(Type type);
        int GetObjectIndex(object o);
    }
}
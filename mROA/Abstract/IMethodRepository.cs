using System.Collections.Generic;
using System.Reflection;

namespace mROA.Abstract
{
    public interface IMethodRepository : IInjectableModule
    {
        MethodInfo GetMethod(int id);
        int RegisterMethod(MethodInfo method);
    
        IEnumerable<MethodInfo> GetMethods();
    }
}
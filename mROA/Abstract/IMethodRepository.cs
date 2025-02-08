using System.Reflection;
using global::System.Collections.Generic;

namespace mROA.Abstract
{
    public interface IMethodRepository : IInjectableModule
    {
        MethodInfo GetMethod(int id);
        int RegisterMethod(MethodInfo method);
    
        IEnumerable<MethodInfo> GetMethods();
    }
}
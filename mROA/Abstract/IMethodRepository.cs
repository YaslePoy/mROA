using System.Reflection;
using mROA.Abstract;

namespace mROA;

public interface IMethodRepository : IInjectableModule
{
    MethodInfo GetMethod(int id);
    int RegisterMethod(MethodInfo method);
    
    IEnumerable<MethodInfo> GetMethods();
}
using System.Reflection;

namespace mROA;

public interface IMethodRepository
{
    MethodInfo GetMethod(int id);
    int RegisterMethod(MethodInfo method);
    
    IEnumerable<MethodInfo> GetMethods();
}
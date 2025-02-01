using System.Reflection;

namespace mROA.Implementation.Test;

public class MockMethodRepository : IMethodRepository
{
    public MethodInfo GetMethod(int id)
    {
        return GetType().GetMethod("MockMethod")!;
    }

    public int RegisterMethod(MethodInfo method)
    {
        return 0;
    }

    public IEnumerable<MethodInfo> GetMethods()
    {
        return [GetType().GetMethod("MockMethod")!];
    }

    private void MockMethod(object x)
    {
    }
}
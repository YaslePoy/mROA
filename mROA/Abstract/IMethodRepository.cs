using System.Reflection;

namespace mROA.Abstract
{
    public interface IMethodRepository : IInjectableModule
    {
        IMethodInvoker GetMethod(int id);
    }
}
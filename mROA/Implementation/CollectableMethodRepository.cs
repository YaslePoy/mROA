using System.Collections.Generic;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class CollectableMethodRepository : IMethodRepository
    {
        private readonly List<IMethodInvoker> _methods = new();

        public void AppendInvokers(IEnumerable<IMethodInvoker> methodInvokers)
        {
            _methods.AddRange(methodInvokers);
        }

        public IMethodInvoker GetMethod(int id)
        {
            return id == -1 ? MethodInvoker.Dispose : _methods[id];
        }
    }
}
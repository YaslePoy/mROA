using System.Collections.Generic;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class CollectableMethodRepository : IMethodRepository
    {
        private List<IMethodInvoker> _methods =  new();
        public void Inject<T>(T dependency)
        {
            
        }

        public void AppendInvokers(IEnumerable<IMethodInvoker> methodInvokers)
        {
            _methods.AddRange(methodInvokers);
        }
        
        public IMethodInvoker GetMethod(int id)
        {
            if (id == -1)
                return MethodInvoker.Dispose;

            if (_methods.Count <= id)
                return null;
            
            return _methods[id];
        }
    }
}
using System;
using System.Collections.Generic;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class CollectableMethodRepository : IMethodRepository
    {
        private readonly List<IMethodInvoker> _methods = new() { MethodInvoker.Dispose };
        private IMethodInvoker[] _baked = Array.Empty<IMethodInvoker>();
        public void AppendInvokers(IEnumerable<IMethodInvoker> methodInvokers)
        {
            _methods.AddRange(methodInvokers);
            _baked = _methods.ToArray();
        }
        public IMethodInvoker GetMethod(int id)
        {
            return _baked[++id];
        }
    }
}
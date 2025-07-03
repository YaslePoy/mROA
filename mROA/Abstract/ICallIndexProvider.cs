using System;
using System.Collections;
using System.Collections.Generic;
using mROA.Implementation;


namespace mROA.Abstract
{
    public interface ICallIndexProvider : IInjectableModule
    {
        Dictionary<Type, Func<int, IRepresentationModule, IEndPointContext, int[], RemoteObjectBase>> Activators { get; }
        void SetupOffset(int offset);
        int[] GetIndices(Type type);
    }

    // public class GeneratedInvokersCollection : IReadOnlyList<IMethodInvoker>
    // {
    //     private readonly List<IMethodInvoker> _invokers = new()
    //     {
    //         
    //     };
    //
    //     public IEnumerator<IMethodInvoker> GetEnumerator()
    //     {
    //         return _invokers.GetEnumerator();
    //     }
    //
    //     IEnumerator IEnumerable.GetEnumerator()
    //     {
    //         return GetEnumerator();
    //     }
    //
    //     public int Count =>  _invokers.Count;
    //
    //     public IMethodInvoker this[int index] => _invokers[index];
    // }
}
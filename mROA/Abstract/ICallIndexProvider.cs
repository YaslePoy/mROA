using System;
using System.Collections.Generic;
using mROA.Implementation;


namespace mROA.Abstract
{
    public interface ICallIndexProvider : IInjectableModule
    {
        Dictionary<Type, Func<int, IRepresentationModule, IEndPointContext, int[], RemoteObjectBase>> Activators
        {
            get;
        }

        void SetupOffset(int offset);
        int[] GetIndices(Type type);
    }
}
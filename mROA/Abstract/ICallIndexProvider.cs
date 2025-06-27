using System;
using System.Collections.Generic;


namespace mROA.Abstract
{
    public interface ICallIndexProvider : IInjectableModule
    {
        int[] GetIndecies(Type type);
    }
} 
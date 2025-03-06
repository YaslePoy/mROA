using System;
using System.Collections.Generic;
using mROA.Implementation;
using mROA.Implementation.Attributes;

namespace Example.Shared
{
    public interface IDataList<T> : IShared, IDisposable
    {
        IReadOnlyList<T> Collection { get; }
        T Get(int index);
        void Add(T item);
        void Set(int index, T item);
    }
}
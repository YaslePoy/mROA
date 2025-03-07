using System;
using System.Collections.Generic;
using Example.Shared;

namespace Example.Backend
{
    public class PagesList : IPagesList
    {
        public IPage Get(int index)
        {
            throw new System.NotImplementedException();
        }

        public void Add(IPage item)
        {
            throw new System.NotImplementedException();
        }

        public void Remove(int index, IPage item)
        {
            throw new System.NotImplementedException();
        }

        public event Action<IPage>? OnAdd;
        public event Action<IPage>? OnRemove;

        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}
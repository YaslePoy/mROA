using System;
using System.Collections.Generic;
using Example.Shared;
using mROA.Abstract;
using mROA.Implementation;

namespace Example.Backend
{
    public class PagesList : RemoteObjectBase, IPagesList
    {
        public IReadOnlyList<IPage> Collection { get; }

        public Example.Shared.IPage this[int index]
        {
            get => GetResultAsync<Example.Shared.IPage>(3, new object[] { index }).GetAwaiter().GetResult();
            set => CallAsync(5, new object[] { index, value }).Wait();
        }

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

        public PagesList(int id, IRepresentationModule representationModule) : base(id, representationModule)
        {
        }
    }
}
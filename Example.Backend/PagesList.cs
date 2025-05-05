using System;
using System.Collections.Generic;
using Example.Shared;
using mROA.Abstract;
using mROA.Implementation;

namespace Example.Backend
{
    public class PagesList : RemoteObjectBase, IPagesList
    {
        public PagesList(int id, IRepresentationModule representationModule,IEndPointContext context) : base(id, representationModule, context)
        {
        }

        public IReadOnlyList<IPage> Collection { get; }

        public IPage this[int index]
        {
            get => GetResultAsync<IPage>(3, new object[] { index }).GetAwaiter().GetResult();
            set => CallAsync(5, new object[] { index, value }).Wait();
        }

        public IPage Get(int index)
        {
            throw new NotImplementedException();
        }

        public void Add(IPage item)
        {
            throw new NotImplementedException();
        }

        public void Remove(int index, IPage item)
        {
            throw new NotImplementedException();
        }

        public event Action<IPage>? OnAdd;
        public event Action<IPage>? OnRemove;

        public void Dispose()
        {
            // TODO release managed resources here
        }

        public void OnAddExternal(IPage p0)
        {
        }

        public void OnRemoveExternal(IPage p0)
        {
        }
    }
}
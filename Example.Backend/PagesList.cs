using System.Collections.Generic;
using Example.Shared;

namespace Example.Backend
{
    public class PagesList : IPagesList
    {
        public IReadOnlyList<IPage> Collection { get; }
        public IPage Get(int index)
        {
            throw new System.NotImplementedException();
        }

        public void Add(IPage item)
        {
            throw new System.NotImplementedException();
        }

        public void Set(int index, IPage item)
        {
            throw new System.NotImplementedException();
        }
    }
}
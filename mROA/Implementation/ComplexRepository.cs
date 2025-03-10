using System;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class ComplexRepository : IContextRepository
    {
        public void Inject<T>(T dependency)
        {
            throw new NotImplementedException();
        }

        public int HostId { get; set; }
        public int ResisterObject<T>(object o, IEndPointContext context)
        {
            throw new NotImplementedException();
        }

        public void ClearObject(ComplexObjectIdentifier id)
        {
            throw new NotImplementedException();
        }

        public T GetObject<T>(ComplexObjectIdentifier id)
        {
            throw new NotImplementedException();
        }

        public object GetSingleObject(Type type)
        {
            throw new NotImplementedException();
        }

        public int GetObjectIndex<T>(object o, IEndPointContext context)
        {
            throw new NotImplementedException();
        }
    }
}
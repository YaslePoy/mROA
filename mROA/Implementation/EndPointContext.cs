using System;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class EndPointContext : IEndPointContext
    {
        public Func<int> OwnerFunc;
        public IContextRepository RealRepository { get; set; }
        public IContextRepository RemoteRepository { get; set; }
        public int HostId { get; set; }

        public int OwnerId
        {
            get => OwnerFunc();
            // ReSharper disable once UnusedMember.Global
            set { OwnerFunc = () => value; }
        }

        public void Inject<T>(T dependency)
        {
        }
    }
}
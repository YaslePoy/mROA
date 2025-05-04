using System;
using mROA.Abstract;
using mROA.Implementation.Backend;

namespace mROA.Implementation
{
    public class EndPointContext : IEndPointContext
    {
        public IContextRepository RealRepository { get; set; }
        public IContextRepository RemoteRepository { get; set; }
        public int HostId { get; set; }

        public int OwnerId { get; set; }

        public void Inject<T>(T dependency)
        {
            switch (dependency)
            {
                case RemoteContextRepository remoteRepository:
                    RemoteRepository = remoteRepository;
                    break;
                case ContextRepository realRepository:
                    RealRepository = realRepository;
                    break;
            }
        }
    }
}
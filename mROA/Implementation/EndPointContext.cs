using mROA.Abstract;
using mROA.Implementation.Backend;

namespace mROA.Implementation
{
    public class EndPointContext : IEndPointContext
    {
        public IInstanceRepository RealRepository { get; set; }
        public IInstanceRepository RemoteRepository { get; set; }
        public int HostId { get; set; }

        public int OwnerId { get; set; }

        public void Inject<T>(T dependency)
        {
            switch (dependency)
            {
                case RemoteInstanceRepository remoteRepository:
                    RemoteRepository = remoteRepository;
                    break;
                case InstanceRepository realRepository:
                    RealRepository = realRepository;
                    break;
            }
        }
    }
}
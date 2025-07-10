using mROA.Abstract;
using mROA.Implementation.Backend;

namespace mROA.Implementation
{
    public class EndPointContext : IEndPointContext
    {
        public IInstanceRepository RealRepository { get; set; }
        public IInstanceRepository RemoteRepository { get; set; }
        public ICallIndexProvider CallIndexProvider { get; set; }
        public CallIndexConfig CallIndexConfig { get; set; }
        public int HostId { get; set; }

        public int OwnerId { get; set; }


        public void Inject(object dependency)
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
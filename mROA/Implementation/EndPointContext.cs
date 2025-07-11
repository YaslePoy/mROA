using mROA.Abstract;

namespace mROA.Implementation
{
    public class EndPointContext : IEndPointContext
    {
        public EndPointContext()
        {
            
        }
        public EndPointContext(IInstanceRepository realRepository, IInstanceRepository remoteRepository)
        {
            RealRepository = realRepository;
            RemoteRepository = remoteRepository;
        }

        public IInstanceRepository RealRepository { get; set; }
        public IInstanceRepository RemoteRepository { get; set; }
        public ICallIndexProvider CallIndexProvider { get; set; }
        public CallIndexConfig CallIndexConfig { get; set; }
        public int HostId { get; set; }

        public int OwnerId { get; set; }
    }
}
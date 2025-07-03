using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IEndPointContext : IInjectableModule
    {
        IInstanceRepository RealRepository { get; set; }
        IInstanceRepository RemoteRepository { get; set;  }
        ICallIndexProvider CallIndexProvider { get; set; } 
        int HostId { get; set; }
        int OwnerId { get; set; }
    }
}
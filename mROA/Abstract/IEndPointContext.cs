using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IEndPointContext : IInjectableModule
    {
        IInstanceRepository RealRepository { get; }
        IInstanceRepository RemoteRepository { get; }
        ICallIndexProvider CallIndexProvider { get; } 
        int HostId { get; set; }
        int OwnerId { get; set; }
    }
}
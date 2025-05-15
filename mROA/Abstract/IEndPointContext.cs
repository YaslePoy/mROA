namespace mROA.Abstract
{
    public interface IEndPointContext : IInjectableModule
    {
        IInstanceRepository RealRepository { get; }
        IInstanceRepository RemoteRepository { get; }
        int HostId { get; set; }
        int OwnerId { get; set; }
    }
}
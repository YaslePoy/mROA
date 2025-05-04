namespace mROA.Abstract
{
    public interface IEndPointContext : IInjectableModule
    {
        IContextRepository RealRepository { get; }
        IContextRepository RemoteRepository { get; }
        int HostId { get; set; }
        int OwnerId { get; set; }
    }
}
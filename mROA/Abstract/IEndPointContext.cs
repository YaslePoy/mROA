namespace mROA.Abstract
{
    public interface IEndPointContext
    {
        IContextRepository RealRepository { get; }
        IContextRepository RemoteRepository { get; }
        int HostId { get; } 
    }
}
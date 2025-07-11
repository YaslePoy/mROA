namespace mROA.Abstract
{
    public interface IEndPointContext
    {
        IInstanceRepository RealRepository { get; set; }
        IInstanceRepository RemoteRepository { get; set; }
        ICallIndexProvider CallIndexProvider { get; set; }
        int HostId { get; set; }
        int OwnerId { get; set; }
    }
}
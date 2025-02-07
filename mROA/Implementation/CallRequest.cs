namespace mROA.Implementation;

public interface ICallRequest
{
    Guid CallRequestId { get; }
    int CommandId { get; }
    int ObjectId { get; }
    object? Parameter { get; }
}

public class DefaultCallRequest : ICallRequest
{
    public Guid CallRequestId { get; set; } = Guid.NewGuid();
    public int CommandId { get; init; }
    public int ObjectId { get; init; } = -1;
    public object? Parameter { get; set; }
}

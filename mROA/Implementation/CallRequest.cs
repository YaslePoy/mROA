namespace mROA.Implementation;

public interface ICallRequest
{
    Guid CallRequestId { get; internal set; }
    int CommandId { get; }
    int ObjectId { get; }
    object Parameter { get; }
}

public class JsonCallRequest : ICallRequest
{
    public Guid CallRequestId { get; set; } = Guid.NewGuid();
    public int CommandId { get; set; }
    public int ObjectId { get; set; } = -1;
    public object? Parameter { get; set; }
}

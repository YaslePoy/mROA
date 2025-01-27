namespace mROA.Implementation;

public interface ICallRequest
{
    Guid CallRequestId { get; }
    int CommandId { get; }
    int ClientId { get; }
    int ObjectId { get; }
    object Parameter { get; }
}

public class JsonCallRequest : ICallRequest
{
    public Guid CallRequestId { get; } = Guid.NewGuid();
    public int CommandId { get; set; }
    public int ClientId { get; set; }
    public int ObjectId { get; set; } = -1;
    public object? Parameter { get; set; }
}

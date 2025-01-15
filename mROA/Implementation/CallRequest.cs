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
// public struct HardCallRequest : ICallRequest
// {
//     public Guid CallRequestId { get; } = Guid.NewGuid();
//     public int CommandId => Command;
//     public int ClientId => Client;
//     public int ObjectId => Object;
//     
//     public int Command;
//     public int Client;
//     public int Object;
//     public object Parameter { get; set; }
//
// }
// public class CallRequest : SingletonCallRequest
// {
//     public override int RequestTypeId => (int)RequestType.NonParametrized;
//     public int ObjectId { get; set; }
// }
//
// public class ParametrizedCallRequest : CallRequest
// {
//     public override int RequestTypeId => (int)RequestType.Parametrized;
//     public object Parameter { get; set; }
// }


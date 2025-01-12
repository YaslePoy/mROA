namespace mROA.Implementation;

public interface ICallRequest
{
    int CommandId { get; }
    int ClientId { get; }
    public int ObjectId { get; }
    public object Parameter { get; }


}


public struct HardCallRequest : ICallRequest
{
    public int CommandId => Command;
    public int ClientId => Client;
    public int ObjectId => Object;
    
    public int Command;
    public int Client;
    public int Object;
    public object Parameter { get; set; }

}
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


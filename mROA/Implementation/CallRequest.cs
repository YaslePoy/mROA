namespace mROA.Implementation;

public interface ICallRequest
{
    int RequestTypeId { get; }
    int CommandId { get; set; }
    int ClientId { get; set; }
}

public class SingletonCallRequest : ICallRequest
{
    public virtual int RequestTypeId => (int)RequestType.Singleton;
    public int CommandId { get; set; }
    public int ClientId { get; set; }
}

public class CallRequest : SingletonCallRequest
{
    public override int RequestTypeId => (int)RequestType.NonParametrized;
    public int ObjectId { get; set; }
}

public class ParametrizedCallRequest : CallRequest
{
    public override int RequestTypeId => (int)RequestType.Parametrized;
    public object Parameter { get; set; }
}

enum RequestType
{
    Singleton,
    NonParametrized,
    Parametrized
}
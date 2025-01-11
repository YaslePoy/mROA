namespace mROA.Implementation;

public interface ICallRequest
{
    int RequestTypeId { get; }
    int CommandId { get; set; }
    int ClientId { get; set; }
}

public class StaticCallRequest : ICallRequest
{
    public virtual int RequestTypeId => (int)RequestType.Static;
    public int CommandId { get; set; }
    public int ClientId { get; set; }
}

public class CallRequest : StaticCallRequest
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
    Static,
    NonParametrized,
    Parametrized
}
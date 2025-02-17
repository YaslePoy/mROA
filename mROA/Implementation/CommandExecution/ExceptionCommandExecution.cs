using System.Text.Json;
using mROA.Abstract;
using mROA.Implementation.Frontend;

namespace mROA.Implementation;

public class ExceptionCommandExecution : ICommandExecution
{
    public Guid Id { get; init; }
    public int ClientId { get; set; }
    public int CommandId { get; init; }
    public required string Exception { get; set; }

    public RemoteException GetException()
    {
        return new RemoteException(Exception) { CallRequestId = Id };
    }
}
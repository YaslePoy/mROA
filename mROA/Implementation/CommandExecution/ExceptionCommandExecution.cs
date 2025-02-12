using mROA.Abstract;

namespace mROA.Implementation;

public class ExceptionCommandExecution : ICommandExecution
{
    public Guid CallRequestId { get; init; }
    public int ClientId { get; set; }
    public int CommandId { get; init; }
    public required string Exception { get; set; }
}
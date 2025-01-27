namespace mROA;

public interface ICommandExecution
{
    Guid CallRequestId { get; init; }
    int ClientId { get; }
    int CommandId { get; }
}
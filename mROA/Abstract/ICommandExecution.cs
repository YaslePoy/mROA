namespace mROA;

public interface ICommandExecution
{
    Guid ExecutionId { get; init; }
    int ClientId { get; set; }
    int CommandId { get; set; }
}
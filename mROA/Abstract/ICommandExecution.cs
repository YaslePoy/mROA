namespace mROA;

public interface ICommandExecution
{
    Guid CallRequestId { get; init; }
    int ClientId { get; set; }
    int CommandId { get; set; }
}
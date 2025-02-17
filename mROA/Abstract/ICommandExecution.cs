namespace mROA.Abstract;

public interface ICommandExecution
{
    Guid Id { get; init; }
    int ClientId { get; set; }
    int CommandId { get; }
}
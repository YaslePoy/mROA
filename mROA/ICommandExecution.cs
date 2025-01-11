namespace mROA;

public interface ICommandExecution
{
    int ExecutionId { get; set; }

    int ClientId { get; set; }
    int CommandId { get; set; }
    
    void Cancel();
}
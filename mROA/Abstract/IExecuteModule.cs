namespace mROA;

public interface IExecuteModule
{
    ICommandExecution Execute(int objectId, int commandId, object parameter);
}
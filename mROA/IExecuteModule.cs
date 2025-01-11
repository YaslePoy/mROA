namespace mROA;

public interface IExecuteModule
{
    void Execute(int objectId, int commandId, object parameter);
}
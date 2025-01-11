using mROA.Implementation;

namespace mROA;

public interface IExecuteModule
{
    ICommandExecution Execute(ICallRequest command);
}
using mROA.Abstract;
using mROA.Implementation;

namespace mROA;

public interface IExecuteModule : IInjectableModule
{
    ICommandExecution Execute(ICallRequest command);
}
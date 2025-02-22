using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IExecuteModule : IInjectableModule
    {
        ICommandExecution Execute(ICallRequest command, IContextRepository contextRepository, IRepresentationModule representationModule);
    }
}
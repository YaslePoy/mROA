using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IExecuteModule : IInjectableModule
    {
        ICommandExecution Execute(ICallRequest command, IInstanceRepository instanceRepository,
            IRepresentationModule representationModule, IEndPointContext context);
    }
}
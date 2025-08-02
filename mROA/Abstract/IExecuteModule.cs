using mROA.Implementation;
using mROA.Implementation.CommandExecution;

namespace mROA.Abstract
{
    public interface IExecuteModule
    {
        ICommandExecution? Execute(CallRequest command, IInstanceRepository instanceRepository,
            IRepresentationModule representationModule, IEndPointContext context);
        ICommandExecution Cancel(CancelRequest command);

    }
}
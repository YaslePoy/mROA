namespace mROA.Implementation;

public class MockExecModule : IExecuteModule
{
    public ICommandExecution Execute(ICallRequest command)
    {
        return new FinalCommandExecution
            { CommandId = command.CommandId, Result = new { A = "wqer", B = 5 }, CallRequestId = command.CallRequestId };
    }
}
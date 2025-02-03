namespace mROA.Implementation.Test;

public class MockExecModule : IExecuteModule
{
    public ICommandExecution Execute(ICallRequest command)
    {
        return new FinalCommandExecution
            { CommandId = command.CommandId, Result = new MockResult { A = "wqer", B = 5 }, CallRequestId = command.CallRequestId };
    }
}

public class MockResult
{
    public string A { get; set; }
    public int B { get; set; }
}
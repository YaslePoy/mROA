namespace mROA.Implementation;

public class MockExecModule : IExecuteModule
{
    public ICommandExecution Execute(ICallRequest command)
    {
        return new FinalCommandExecution
            { ClientId = command.ClientId, CommandId = command.CommandId, Result = new { A = "wqer", B = 5 } };
    }
}
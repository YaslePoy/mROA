using mROA.Implementation;

namespace mROA.Example;

[SharedObjectSingleton]
public class TestController : ITestController
{
    public ITestController? ReturnIfNotNull;
    private int _bOut = 5;

    public void A()
    {
        Console.WriteLine("A called");
    }

    public Task AAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public int B()
    {
        return _bOut++;
    }

    public Task<int> BAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(5);
    }

    public TransmittedSharedObject<ITestController> SharedObjectTransmitionTest()
    {
        return new TransmissionTestController { ReturnIfNotNull = ReturnIfNotNull ?? new TestController() };
    }

    public int Parametrized(TestParameter parameter)
    {
        return parameter.A + parameter.LinkedObject.Value.Test();
    }

    public TransmittedSharedObject<ITestParameter> GetTestParameter()
    {
        return new TestParameterInstance();
    }
}
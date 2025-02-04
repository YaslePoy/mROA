using mROA.Implementation;

namespace mROA.Example;

public class TransmissionTestController : ITestController
{
    public ITestController ReturnIfNotNull;

    public void A()
    {
        Console.WriteLine("A called alternative");
    }

    public Task AAsync(CancellationToken cancellationToken)
    {
        Thread.Sleep(500);
        return Task.CompletedTask;
    }

    public int B()
    {
        return 456789;
    }

    public Task<int> BAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(465789);
    }

    public TransmittedSharedObject<ITestController> SharedObjectTransmitionTest()
    {
        TransmittedSharedObject<ITestController> x = new TestController();


        return new TestController { ReturnIfNotNull = this };
    }

    public int Parametrized(TestParameter parameter)
    {
        return parameter.A * parameter.LinkedObject.Value.Test();
    }

    public TransmittedSharedObject<ITestParameter> GetTestParameter()
    {
        return new TestParameterInstance();
    }
}
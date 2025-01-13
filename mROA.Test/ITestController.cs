using mROA.Implementation;

namespace mROA.Test;

[SharedObjectInterafce]
public interface ITestController
{
    void A();
    Task AAsync(CancellationToken cancellationToken);
    int B();
    Task<int> BAsync(CancellationToken cancellationToken);

    TransmittedSharedObject<ITestController> SharedObjectTransmitionTest();
}

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
        return new TransmittionTestController { ReturnIfNotNull = ReturnIfNotNull ?? new TestController() };
    }
}

public class TransmittionTestController : ITestController
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
        return new TestController { ReturnIfNotNull = this };
    }
}
using mROA.Implementation;

namespace mROA.Test;

[SharedObjectInterafce]
public interface ITestController
{
    void A();
    Task AAsync(CancellationToken cancellationToken);
    int B();
    Task<int> BAsync(CancellationToken cancellationToken);
}

[SharedObjectSingleton]
public class TestController : ITestController
{
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
}
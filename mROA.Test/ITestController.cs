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
    int Parametrized(TestParameter parameter);
    TransmittedSharedObject<ITestParameter> GetTestParameter();
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
        return new TransmissionTestController { ReturnIfNotNull = ReturnIfNotNull ?? new TestController() };
    }

    public int Parametrized(TestParameter parameter)
    {
        return parameter.A + parameter.LinkedObject.Value.Test;
    }

    public TransmittedSharedObject<ITestParameter> GetTestParameter()
    {
        return new TestParameterInstance();
    }
}

[SharedObjectInterafce]
public interface ITestParameter
{
    public int Test { get; }
}

public class TestParameterInstance : ITestParameter
{
    public int Test => 10;
}

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
        return parameter.A * parameter.LinkedObject.Value.Test;
    }

    public TransmittedSharedObject<ITestParameter> GetTestParameter()
    {
        return new TestParameterInstance();
    }
}

public class TestParameter
{
    public int A { get; set; }
    public TransmittedSharedObject<ITestParameter> LinkedObject { get; set; }
}

public class TestControllerRemoteEndoint : ITestController, IRemoteObject
{
    public TestControllerRemoteEndoint(int id)
    {
        _id = id;
    }
    public void A()
    {
        
    }

    public Task AAsync(CancellationToken cancellationToken)
    {
        
    }

    public int B()
    {
        
    }

    public Task<int> BAsync(CancellationToken cancellationToken)
    {
        
    }

    public TransmittedSharedObject<ITestController> SharedObjectTransmitionTest()
    {
        
    }

    public int Parametrized(TestParameter parameter)
    {
        
    }

    public TransmittedSharedObject<ITestParameter> GetTestParameter()
    {
        
    }

    private int _id;
    public int Id => _id;
}
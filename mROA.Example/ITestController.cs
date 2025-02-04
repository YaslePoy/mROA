using mROA.Implementation;

namespace mROA.Example;

[SharedObjectInterface]
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
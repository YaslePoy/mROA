using mROA.Implementation;

namespace mROA.Example;

public class TestParameter
{
    public int A { get; set; }
    public TransmittedSharedObject<ITestParameter> LinkedObject { get; set; }
}
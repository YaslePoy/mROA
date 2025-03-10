using mROA.Implementation;

namespace mROA.Test;

public class UnSOization
{
    private ComplexObjectIdentifier _uoi;

    [SetUp]
    public void Setup()
    {
        _uoi = new ComplexObjectIdentifier
        {
            ContextId = -123, OwnerId = 123
        };
    }

    [Test]
    public void FlatTest()
    {
        var flat = _uoi.Flat;
        var next = new ComplexObjectIdentifier { Flat = flat };

        Assert.That(_uoi, Is.EqualTo(next));
    }
}
using mROA.Implementation;

namespace mROA.Test;

public class UnSOization
{
    private UniversalObjectIdentifier _uoi;
    
    [SetUp]
    public void Setup()
    {
        _uoi = new UniversalObjectIdentifier
        {
            ContextId = -123, OwnerId = 123
        };
    }

    [Test]
    public void FlatTest()
    {
        var flat = _uoi.Flat;
        var next = new UniversalObjectIdentifier { Flat = flat };
        
        Assert.That(_uoi, Is.EqualTo(next));
    }
}
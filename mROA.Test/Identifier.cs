using mROA.Implementation;

namespace mROA.Test;

[TestFixture]
public class Identifier
{
    [Test]
    public void TestParse()
    {
        var id = new ComplexObjectIdentifier(-1, -1);
        var flat = id.Flat;
        var next = new ComplexObjectIdentifier { Flat = flat };
        if (id.Equals(next))
        {
            Assert.Pass();
        }
        else
        {
            Assert.Fail();
        }
    }
}
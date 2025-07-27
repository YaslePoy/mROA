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

    [Test]
    public void RequestIdTest()
    {
        var id = RequestId.Generate();
        Assert.Pass(id.ToString());
    }
    
    [Test]
    public void EqualsTest()
    {
        var id = RequestId.Generate();
        var id2 = new RequestId { P0 = id.P0, P1 = id.P1 };
        Assert.That(id2, Is.EqualTo(id));
    }
    
    [Test]
    public void ByteString()
    {
        var id = RequestId.Generate();
        var binary = id.ToByteArray();
        var reverced = new RequestId(binary);
        Assert.That(reverced, Is.EqualTo(id));
    }
}
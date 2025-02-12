using mROA.Implementation;
using mROA.Implementation.Attributes;

namespace Example.Shared;

[SharedObjectInterface]
public interface ILoadTest
{
    int Next(int last);
    int Last(int next);
    void C();
    void A();
}


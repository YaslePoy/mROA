using Example.Shared;
using mROA.Implementation;
using mROA.Implementation.Attributes;

namespace Example.Backend;

[SharedObjectSingleton]
public class LoadTestImp : ILoadTest
{
    public int Next(int last)
    {
        return last + 1;
    }
}
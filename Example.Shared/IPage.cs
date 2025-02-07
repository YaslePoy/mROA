using mROA.Implementation;
using mROA.Implementation.Attributes;

namespace Example.Shared;

[SharedObjectInterface]
public interface IPage
{
    byte[] GetData();
}
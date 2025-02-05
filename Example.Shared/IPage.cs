using mROA.Implementation;

namespace Example.Shared;

[SharedObjectInterface]
public interface IPage
{
    byte[] GetData();
}
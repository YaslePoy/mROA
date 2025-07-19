using mROA.Abstract;
using mROA.Implementation.Attributes;

namespace Example.Shared
{
    [SharedObjectInterface]
    public interface IPage : IShared
    {
        byte[] GetData();
    }
}
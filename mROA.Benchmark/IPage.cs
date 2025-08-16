using mROA.Abstract;
using mROA.Implementation.Attributes;

namespace mROA.Benchmark
{
    [SharedObjectInterface]
    public interface IPage : IShared
    {
        byte[] GetData();
    }
}
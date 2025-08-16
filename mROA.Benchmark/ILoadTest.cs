using mROA.Abstract;
using mROA.Implementation.Attributes;

namespace mROA.Benchmark
{
    [SharedObjectInterface]
    public interface ILoadTest : IShared
    {
        Task<int> Next(int last);
        int Last(int next);
        void C();
        void A();
        Task AsyncTest(CancellationToken token = default);
    }
}
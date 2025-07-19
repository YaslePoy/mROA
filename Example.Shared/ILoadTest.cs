using System.Threading;
using System.Threading.Tasks;
using mROA.Abstract;
using mROA.Implementation.Attributes;

namespace Example.Shared
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
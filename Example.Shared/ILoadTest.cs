using System.Threading;
using System.Threading.Tasks;
using mROA.Implementation;
using mROA.Implementation.Attributes;

namespace Example.Shared
{
    [SharedObjectInterface]
    public interface ILoadTest : IShared
    {
        int Next(int last);
        int Last(int next);
        void C();
        void A();

        Task AsyncTest(CancellationToken token = default);
    }
}


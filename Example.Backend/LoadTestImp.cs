using System;
using System.Threading;
using System.Threading.Tasks;
using Example.Shared;
using mROA.Implementation.Attributes;

namespace Example.Backend
{
    [SharedObjectSingleton]
    public class LoadTestImp : ILoadTest
    {
        public int Next(int last)
        {
            return last + 1;
        }

        public int Last(int next)
        {
            return next - 1;
        }

        public void C()
        {
            throw new NotImplementedException();
        }

        public void A()
        {
            throw new NotImplementedException();
        }

        public async Task AsyncTest(CancellationToken token)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), token);
        }
    }
}
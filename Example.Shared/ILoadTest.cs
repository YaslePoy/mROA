using System.Threading;
using System.Threading.Tasks;
using mROA.Abstract;
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
    
    partial class LoadTestProxy2
        : RemoteObjectBase, ILoadTest
    {
        public LoadTestProxy2(int id, IRepresentationModule representationModule, IEndPointContext context) 
            : base(id, representationModule, context)
        {
        }

        public void A(){
            CallAsync(0).Wait();
        }

        public async System.Threading.Tasks.Task AsyncTest(System.Threading.CancellationToken token){
            await CallAsync(1, cancellationToken : token);
        }

        public void C(){
            CallAsync(2).Wait();
        }

        public System.Int32 Last(System.Int32 next){
            return GetResultAsync<System.Int32>(3, new System.Object[] { next }).GetAwaiter().GetResult();
        }

        public System.Int32 Next(System.Int32 last){
            return GetResultAsync<System.Int32>(4, new System.Object[] { last }).GetAwaiter().GetResult();
        }

        
    }
}
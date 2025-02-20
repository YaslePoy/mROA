using System.Threading;
using System.Threading.Tasks;
using mROA.Implementation;
using mROA.Implementation.Attributes;

namespace Example.Shared
{
    [SharedObjectInterface]
    public interface IPrinter
    {
        string GetName();
        Task<SharedObject<IPage>> Print(string text, CancellationToken cancellationToken);
    
    }
}
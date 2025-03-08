using System;
using System.Threading;
using System.Threading.Tasks;
using mROA.Implementation;
using mROA.Implementation.Attributes;

namespace Example.Shared
{
    [SharedObjectInterface]
    public interface IPrinter : IDisposable, IShared
    {
        decimal Resource { get; set; }
        string GetName();
        Task<IPage> Print(string text, bool someParameter, CancellationToken cancellationToken);
        event Action<IPage> OnPrint;
    }
}
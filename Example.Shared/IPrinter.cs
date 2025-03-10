using System;
using System.Threading;
using System.Threading.Tasks;
using mROA.Implementation;
using mROA.Implementation.Attributes;

namespace Example.Shared
{
    [SharedObjectInterface]
    public partial interface IPrinter : IDisposable, IShared
    {
        double Resource { get; set; }
        string GetName();
        Task<IPage> Print(string text, bool someParameter, RequestContext context, CancellationToken cancellationToken);
        event Action<IPage> OnPrint;
    }
}
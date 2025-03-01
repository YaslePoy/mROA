using System;
using System.Threading;
using System.Threading.Tasks;
using mROA.Implementation;
using mROA.Implementation.Attributes;

namespace Example.Shared
{
    [SharedObjectInterface]
    public interface IPrinter : IDisposable
    {
        string GetName();
        Task<IPage> Print(string text, CancellationToken cancellationToken);
    
    }
}
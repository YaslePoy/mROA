using mROA.Implementation;
using mROA.Implementation.Attributes;

namespace Example.Shared;

[SharedObjectInterface]
public interface IPrinter
{
    string GetName();
    Task<TransmittedSharedObject<IPage>> Print(string text, CancellationToken cancellationToken);
    
}
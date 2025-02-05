using mROA.Implementation;

namespace Example.Shared;

[SharedObjectInterface]
public interface IPrinter
{
    string GetName();
    Task<TransmittedSharedObject<IPage>> Print(string text);

}
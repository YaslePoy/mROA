using mROA.Implementation;

namespace Example.Shared;

[SharedObjectInterface]
public interface IPrinter
{
    string GetName();
    TransmittedSharedObject<IPage> Print(string text);

}
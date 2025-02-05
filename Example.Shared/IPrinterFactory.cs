using mROA.Implementation;

namespace Example.Shared;

[SharedObjectInterface]
public interface IPrinterFactory
{
    TransmittedSharedObject<IPrinter> Create(string printerName);
}
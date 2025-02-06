using Example.Backend;
using Example.Shared;
using mROA.Implementation;

namespace Example.Backend;

[SharedObjectSingleton]
public class PrinterFactory : IPrinterFactory
{
    public TransmittedSharedObject<IPrinter> Create(string printerName)
    {
        return new Printer {Name = printerName};
    }
}
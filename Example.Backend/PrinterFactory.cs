using Example.Backend;
using Example.Shared;
using mROA.Implementation;
using mROA.Implementation.Attributes;

namespace Example.Backend;

[SharedObjectSingleton]
public class PrinterFactory : IPrinterFactory
{
    public TransmittedSharedObject<IPrinter> Create(string printerName)
    {
        return new Printer {Name = printerName};
    }
}
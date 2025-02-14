using Example.Backend;
using Example.Shared;
using mROA.Implementation;
using mROA.Implementation.Attributes;

namespace Example.Backend;

[SharedObjectSingleton]
public class PrinterFactory : IPrinterFactory
{
    private List<IPrinter> _printers = new();

    public SharedObject<IPrinter> Create(string printerName)
    {
        return new Printer { Name = printerName };
    }

    public void Register(SharedObject<IPrinter> printer)
    {
        _printers.Add(printer.Value);
    }

    public SharedObject<IPrinter> GetPrinterByName(string printerName)
    {
        return new SharedObject<IPrinter>(_printers.Find(i => i.GetName() == printerName)!);
    }

    public string[] CollectAllNames()
    {
        return _printers.Select(i => i.GetName()).ToArray();
    }
}
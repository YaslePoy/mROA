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
        Console.WriteLine("Creating printer");
        return new Printer { Name = printerName };
    }

    public void Register(SharedObject<IPrinter> printer)
    {
        _printers.Add(printer.Value);
        Console.WriteLine("Registered printer");
    }

    public SharedObject<IPrinter> GetPrinterByName(string printerName)
    {
        Console.WriteLine("Getting printer");
        return new SharedObject<IPrinter>(_printers.Find(i => i.GetName() == printerName)!);
    }

    public SharedObject<IPrinter> GetFirstPrinter()
    {
        return new SharedObject<IPrinter>(_printers.First());
    }

    public string[] CollectAllNames()
    {
        Console.WriteLine("Collecting all printers");
        return _printers.Select(i => i.GetName()).ToArray();
    }
}
using mROA.Implementation;
using mROA.Implementation.Attributes;

namespace Example.Shared
{
    [SharedObjectInterface]
    public interface IPrinterFactory
    {
        IPrinter Create(string printerName);
        void Register(IPrinter printer);
        IPrinter GetPrinterByName(string printerName);
        IPrinter GetFirstPrinter();
        string[] CollectAllNames();

    }
}
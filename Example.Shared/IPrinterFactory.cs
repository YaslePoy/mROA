using mROA.Abstract;
using mROA.Implementation.Attributes;

namespace Example.Shared
{
    [SharedObjectInterface]
    public interface IPrinterFactory : IShared
    {
        IPrinter Create(string printerName);
        void Register(IPrinter printer);
        IPrinter GetPrinterByName(string printerName);
        IPrinter GetFirstPrinter();
        string[] CollectAllNames();
    }
}
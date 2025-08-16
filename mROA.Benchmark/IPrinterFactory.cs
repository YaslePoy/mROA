using mROA.Abstract;
using mROA.Implementation.Attributes;

namespace mROA.Benchmark
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
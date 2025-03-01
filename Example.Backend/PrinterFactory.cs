using System;
using System.Collections.Generic;
using System.Linq;
using Example.Shared;
using mROA.Implementation;
using mROA.Implementation.Attributes;

namespace Example.Backend
{
    [SharedObjectSingleton]
    public class PrinterFactory : IPrinterFactory
    {
        private List<IPrinter> _printers = new List<IPrinter>();

        public IPrinter Create(string printerName)
        {
            Console.WriteLine("Creating printer");
            return new Printer { Name = printerName };
        }

        public void Register(IPrinter printer)
        {
            _printers.Add(printer);
            Console.WriteLine("Registered printer");
        }

        public IPrinter GetPrinterByName(string printerName)
        {
            Console.WriteLine("Getting printer");
            return (_printers.Find(i => i.GetName() == printerName)!);
        }

        public IPrinter GetFirstPrinter()
        {
            return _printers.First();
        }

        public string[] CollectAllNames()
        {
            Console.WriteLine("Collecting all printers");
            return _printers.Select(i => i.GetName()).ToArray();
        }
    }
}
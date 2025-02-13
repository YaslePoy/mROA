using System.Collections.Frozen;
using mROA.Implementation;
using mROA.Implementation.Attributes;

namespace Example.Shared;

[SharedObjectInterface]
public interface IPrinterFactory
{
    SharedObject<IPrinter> Create(string printerName);
}


using System.Text;
using mROA.Implementation;

namespace Example.Shared;

[SharedObjectInterface]
public interface IPrinterFactory
{
    TransmittedSharedObject<IPrinter> Create(string printerName);
}


[SharedObjectSingleton]
public class PrinterFactory : IPrinterFactory
{
    public TransmittedSharedObject<IPrinter> Create(string printerName)
    {
        return new Printer(){Name = printerName};
    }
}


[SharedObjectInterface]
public interface IPrinter
{
    string GetName();
    TransmittedSharedObject<IPage> Print(string text);

}

public class Printer : IPrinter
{
    public string Name;
    public string GetName()
    {
        return Name;
    }

    public TransmittedSharedObject<IPage> Print(string text)
    {
        return new Page {Text = text};
    }
}

[SharedObjectInterface]
public interface IPage
{
    byte[] GetData();
}

public class Page : IPage
{
    public string Text;
    public byte[] GetData()
    {
        return Encoding.UTF8.GetBytes(Text);
    }
}
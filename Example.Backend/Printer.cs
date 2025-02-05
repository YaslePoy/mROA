using Example.Shared;
using mROA.Implementation;

namespace Example.Backend;


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
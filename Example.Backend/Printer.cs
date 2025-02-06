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

    public async Task<TransmittedSharedObject<IPage>> Print(string text, CancellationToken cancellationToken = default)
    {
        return new Page {Text = text};
    }
}
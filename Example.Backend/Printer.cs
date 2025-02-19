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

    public async Task<SharedObject<IPage>> Print(string text, CancellationToken cancellationToken = default)
    {
        // throw new Exception("The method or operation is not implemented.");
        // var sharedObject = new Page { Text = text };
        return new Page { Text = text };
    }
}
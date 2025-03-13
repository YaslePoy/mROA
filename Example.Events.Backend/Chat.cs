using Example.Events.Shared;
using mROA.Implementation;

namespace Example.Events.Backend;

public class Chat : IChat
{
    public void PostSymbol(string symbol, RequestContext context)
    {
        OnCharPosted?.Invoke(symbol, context);
    }

    public event Action<string, RequestContext>? OnCharPosted;

    public void OnCharPostedExternal(string p0, RequestContext p1)
    {
    }
}
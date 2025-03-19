using System;
using mROA.Implementation;
using mROA.Implementation.Attributes;

namespace Example.Events.Shared
{
    [SharedObjectInterface]
    public partial interface IChat : IShared
    {
        void PostSymbol(string symbol, RequestContext context = default);
        event Action<string, RequestContext> OnCharPosted;
    }
}
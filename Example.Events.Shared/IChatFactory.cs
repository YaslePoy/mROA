using System;
using mROA.Implementation;
using mROA.Implementation.Attributes;

namespace Example.Events.Shared
{
    [SharedObjectInterface]
    public interface IChatFactory : IShared
    {
        IChat GetChat(Guid id);
    }
}
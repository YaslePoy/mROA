using Example.Events.Shared;
using mROA.Implementation.Attributes;

namespace Example.Events.Backend;

[SharedObjectSingleton]
public class ChatFactory : IChatFactory
{
    private static readonly Dictionary<Guid, IChat> Chats = new();

    public IChat GetChat(Guid id)
    {
        if (Chats.TryGetValue(id, out var chat))
        {
            return chat;
        }

        var created = new Chat();
        Chats.Add(id, created);
        return created;
    }
}
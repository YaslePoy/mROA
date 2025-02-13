using mROA.Abstract;

namespace mROA.Implementation.Backend;

public class MultiClientOwnershipRepository : IOwnershipRepository
{
    private Dictionary<int, int> _ownerships = new();

    public int GetOwnershipId()
    {
        return _ownerships.GetValueOrDefault(Environment.CurrentManagedThreadId, -1);
    }

    public void RegisterOwnership(int ownershipId, int threadId)
    {
        _ownerships.TryAdd(ownershipId, threadId);
    }

    public void FreeOwnership(int ownershipId)
    {
        _ownerships.Remove(ownershipId);
    }
}
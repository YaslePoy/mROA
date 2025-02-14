using mROA.Abstract;

namespace mROA.Implementation.Backend;

public class MultiClientOwnershipRepository : IOwnershipRepository
{
    private Dictionary<int, int> _ownerships = new();

    public int GetOwnershipId()
    {
        return _ownerships.GetValueOrDefault(Environment.CurrentManagedThreadId, 0);
    }

    public int GetHostOwnershipId()
    {
        return 0;
    }

    public void RegisterOwnership(int ownershipId)
    {
        _ownerships.TryAdd(Environment.CurrentManagedThreadId, ownershipId);
    }

    public void FreeOwnership()
    {
        _ownerships.Remove(Environment.CurrentManagedThreadId);
    }
}
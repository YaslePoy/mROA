using mROA.Abstract;

namespace mROA.Implementation.Frontend;

public class StaticOwnershipRepository(int id) : IOwnershipRepository
{
    public int GetOwnershipId()
    {
        return id;
    }
}
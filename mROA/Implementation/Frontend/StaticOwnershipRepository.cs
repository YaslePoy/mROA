using mROA.Abstract;

namespace mROA.Implementation.Frontend
{
    public class StaticOwnershipRepository : IOwnershipRepository
    {
        private readonly int _id;

        public StaticOwnershipRepository(int id)
        {
            _id = id;
        }

        public int GetOwnershipId()
        {
            return _id;
        }

        public int GetHostOwnershipId()
        {
            return _id;
        }
    }
}
using mROA.Abstract;

namespace mROA.Implementation.Backend
{
    public class BackendIdentityGenerator : IIdentityGenerator
    {
        private int _currentId;

        public int GetNextIdentity()
        {
            return -++_currentId;
        }

        public void Inject(object dependency)
        {
        }
    }
}
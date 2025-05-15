namespace mROA.Abstract
{
    public interface IContextRepositoryHub
    {
        IInstanceRepository GetRepository(int clientId);
        void FreeRepository(int clientId);
    }
}
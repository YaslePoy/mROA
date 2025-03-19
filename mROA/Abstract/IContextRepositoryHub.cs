namespace mROA.Abstract
{
    public interface IContextRepositoryHub
    {
        IContextRepository GetRepository(int clientId);
    }
}
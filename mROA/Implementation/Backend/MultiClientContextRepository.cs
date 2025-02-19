using mROA.Abstract;

namespace mROA.Implementation.Backend;

public class MultiClientContextRepository(Func<int, IContextRepository> produceRepository) : IContextRepository, IContextRepositoryHub
{
    private Dictionary<int, IContextRepository> _repositories = new();

    private IContextRepository GetRepositoryByClientId(int clientId)
    {
        if (_repositories.TryGetValue(clientId, out var repository))
            return repository;
        
        var created = produceRepository(clientId);
        _repositories.Add(clientId, created);
        return created;
    }
    public void Inject<T>(T dependency)
    {
    }

    public int ResisterObject(object o)
    {
        return GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId()).ResisterObject(o);
    }

    public void ClearObject(int id)
    {
        GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId()).ClearObject(id);
    }

    public object GetObject(int id)
    {
        return GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId()).GetObject(id);
    }

    public T? GetObject<T>(int id)
    {
        return GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId()).GetObject<T>(id);
    }

    public T GetSingleObject<T>()
    {
        var result = GetSingleObject(typeof(T));
        return (T)result;
    }

    public object GetSingleObject(Type type)
    {
        return GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId()).GetSingleObject(type); 
    }

    public int GetObjectIndex(object o)
    {
        return GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId()).GetObjectIndex(o); 
    }

    public IContextRepository GetRepository(int clientId)
    {
        return GetRepositoryByClientId(clientId);
    }
}
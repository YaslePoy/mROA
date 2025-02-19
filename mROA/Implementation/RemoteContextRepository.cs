using System.Collections.Frozen;
using mROA.Abstract;

namespace mROA.Implementation;

public class RemoteContextRepository : IContextRepository
{
    private IRepresentationModuleProducer? _representationProducer;
    public static FrozenDictionary<Type, Type> RemoteTypes = FrozenDictionary<Type, Type>.Empty;
    public int ResisterObject(object o)
    {
        throw new NotSupportedException();
    }

    public void ClearObject(int id)
    {
        throw new NotSupportedException();
    }

    public object GetObject(int id)
    {
        throw new NotSupportedException();
    }

    public T GetObject<T>(int id)
    {
        if (_representationProducer == null)
            throw new NullReferenceException("representation producer is not initialized");
        
        if (!RemoteTypes.TryGetValue(typeof(T), out var remoteType)) throw new NotSupportedException();
        var remote = (T)Activator.CreateInstance(remoteType, id, _representationProducer.Produce(TransmissionConfig.OwnershipRepository.GetOwnershipId()))!;
        return remote;
    }
    public T GetSingleObject<T>()
    {
        var obj = GetSingleObject(typeof(T));
        return (T)obj;
    }
    
    public object GetSingleObject(Type type)
    {
        if (_representationProducer == null)
            throw new NullReferenceException("representation producer is not initialized");
        
        return Activator.CreateInstance(RemoteTypes[type], -1, _representationProducer.Produce(TransmissionConfig.OwnershipRepository.GetOwnershipId()))!;
    }

    public int GetObjectIndex(object o)
    {
        if (o is RemoteObjectBase remote)
        {
            return remote.Id;
        }
        throw new NotSupportedException();
    }

    public void Inject<T>(T dependency)
    {
        if (dependency is IRepresentationModuleProducer serialisationModule)
            _representationProducer = serialisationModule;
    }
}
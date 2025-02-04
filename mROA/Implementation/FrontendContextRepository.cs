using System.Collections.Frozen;

namespace mROA.Implementation;

public class FrontendContextRepository(Dictionary<Type, Type> remoteTypes, ISerialisationModule.IFrontendSerialisationModule serialisationModule) : IContextRepository
{
    private FrozenDictionary<Type, Type> _remoteTypes = remoteTypes.ToFrozenDictionary();
    
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
        if (_remoteTypes.TryGetValue(typeof(T), out var remoteType))
        {
            var remote = (T)Activator.CreateInstance(remoteType, id, serialisationModule)!;
            return remote;
        }
        throw new NotSupportedException();
    }

    public object GetSingleObject(Type type)
    {
        return Activator.CreateInstance(remoteTypes[type], -1, serialisationModule)!;
    }

    public int GetObjectIndex(object o)
    {
        if (o is IRemoteObject remote)
        {
            return remote.Id;
        }
        throw new NotSupportedException();
    }
}
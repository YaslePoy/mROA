using System.Collections.Frozen;
using System.Collections.Immutable;
using mROA.Abstract;

namespace mROA.Implementation;

public class RemoteContextRepository : IContextRepository
{
    private ISerialisationModule.IFrontendSerialisationModule _serialisationModule;
    public static FrozenDictionary<Type, Type> RemoteTypes;
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
        if (RemoteTypes.TryGetValue(typeof(T), out var remoteType))
        {
            var remote = (T)Activator.CreateInstance(remoteType, id, _serialisationModule)!;
            return remote;
        }
        throw new NotSupportedException();
    }

    public object GetSingleObject(Type type)
    {
        return Activator.CreateInstance(RemoteTypes[type], -1, _serialisationModule)!;
    }

    public int GetObjectIndex(object o)
    {
        if (o is IRemoteObject remote)
        {
            return remote.Id;
        }
        throw new NotSupportedException();
    }

    public void Inject<T>(T dependency)
    {
        if (dependency is ISerialisationModule.IFrontendSerialisationModule serialisationModule)
            _serialisationModule = serialisationModule;
    }
}
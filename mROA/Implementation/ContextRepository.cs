using System.Collections.Frozen;
using System.Reflection;

namespace mROA.Implementation;

public class ContextRepository : IContextRepository
{
    private FrozenDictionary<int, object?> _singletons;
    private object[] _storage;
    private int _lastIndex;

    private Task<int>? _lastIndexFinder;

    const int StartupSize = 1024;
    const int GrowSize = 128;


    public ContextRepository()
    {
        _storage = new object[StartupSize];
    }

    public void FillSingletons(Assembly assembly)
    {
        var types = assembly.GetTypes().Where(type =>
            type is { IsClass: true, IsAbstract: false, IsGenericType: false } &&
            type.GetCustomAttributes(typeof(SharedObjectSingletonAttribute), true).Length > 0);
        _singletons = types.ToFrozenDictionary(t => t.GetInterfaces().FirstOrDefault(i => i.GetCustomAttributes(typeof(SharedObjectInterafceAttribute), true).Length > 0)!.GetHashCode(), Activator.CreateInstance);
    }

    public int ResisterObject(object o)
    {
        if (_lastIndexFinder is not null)
            _lastIndexFinder.Wait();

        var oldIndex = _lastIndex;
        _lastIndex = _lastIndexFinder.Result;

        _storage[oldIndex] = o;

        _lastIndexFinder = FindLastIndex();

        return _lastIndex;
    }

    public void ClearObject(int id)
    {
        _storage[id] = null;
        _lastIndexFinder = Task.FromResult(id);
    }

    public object GetObject(int id)
    {
        return _storage.Length == -1 || _storage.Length <= id ? null : _storage[id];
    }

    public object GetSingleObject(Type type)
    {
        return _singletons.TryGetValue(type.GetHashCode(), out var value) ? value : null;
    }

    private async Task<int> FindLastIndex()
    {
        for (int i = 0; i < _storage.Length; i++)
        {
            if (_storage[i] is null)
                return i;
        }

        var nextStorage = new object[_storage.Length + GrowSize];
        Array.Copy(_storage, nextStorage, _storage.Length);
        return _storage.Length;
    }
}
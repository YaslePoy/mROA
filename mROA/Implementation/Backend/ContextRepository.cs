using System.Collections.Frozen;
using System.Reflection;

namespace mROA.Implementation;

public class ContextRepository : IContextRepository
{
    private FrozenDictionary<int, object?> _singletons;
    private object[] _storage;

    private Task<int> _lastIndexFinder = Task.FromResult(0);

    const int StartupSize = 1024;
    const int GrowSize = 128;


    public ContextRepository()
    {
        _storage = new object[StartupSize];
    }

    public void FillSingletons(params Assembly[] assembly)
    {
        var types = assembly.SelectMany(x => x.GetTypes()).Where(type =>
            type is { IsClass: true, IsAbstract: false, IsGenericType: false } &&
            type.GetCustomAttributes(typeof(SharedObjectSingletonAttribute), true).Length > 0);
        _singletons =
            types.ToFrozenDictionary(
                t => t.GetInterfaces().FirstOrDefault(i =>
                    i.GetCustomAttributes(typeof(SharedObjectInterfaceAttribute), true).Length > 0)!.GetHashCode(),
                Activator.CreateInstance);
    }

    public int ResisterObject(object o)
    {
        if (!_lastIndexFinder.IsCompleted)
            _lastIndexFinder.Wait();

        _storage[_lastIndexFinder.Result] = o;

        var last = _lastIndexFinder.Result;
        _lastIndexFinder = FindLastIndex();

        return last;
    }

    public void ClearObject(int id)
    {
        _storage[id] = null;
        _lastIndexFinder = Task.FromResult(id);
    }

    public object? GetObject(int id)
    {
        return id == -1 || _storage.Length <= id ? null : _storage[id];
    }

    public T GetObject<T>(int id)
    {
        return id == -1 || _storage.Length <= id ? throw new NullReferenceException("Cannot find that object. It is null"): (T)_storage[id];
    }

    public object GetSingleObject(Type type)
    {
        return _singletons.GetValueOrDefault(type.GetHashCode()) ?? throw new ArgumentException("Unregistered singleton type");
    }

    public int GetObjectIndex(object o)
    {
        var index = Array.IndexOf(_storage, o);
        if (index == -1)
            return ResisterObject(o);
        return index;
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
    public void Inject<T>(T dependency)
    {
    }

    public void Bake()
    {
    }
}
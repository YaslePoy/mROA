using System.Collections.Frozen;
using System.Reflection;
using mROA.Abstract;
using mROA.Implementation.Attributes;

namespace mROA.Implementation.Backend;

public class ContextRepository : IContextRepository
{
    private FrozenDictionary<int, object?>? _singletons;
    private object?[] _storage = new object[StartupSize];

    private Task<int> _lastIndexFinder = Task.FromResult(0);

    private const int StartupSize = 1024;
    private const int GrowSize = 128;


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
        _lastIndexFinder = Task.Run(FindLastIndex);

        return last;
    }

    public void ClearObject(int id)
    {
        _storage[id] = null;
        _lastIndexFinder = Task.FromResult(id);
    }

    public object GetObject(int id)
    {
        return (id == -1 || _storage.Length <= id ? null : _storage[id]) ?? throw new NullReferenceException();
    }

    public T GetObject<T>(int id)
    {
        return id == -1 || _storage.Length <= id ? throw new NullReferenceException("Cannot find that object. It is null"): (T)_storage[id]!;
    }

    public object GetSingleObject(Type type)
    {
        return _singletons!.GetValueOrDefault(type.GetHashCode()) ?? throw new ArgumentException("Unregistered singleton type");
    }

    public int GetObjectIndex(object o)
    {
        var index = Array.IndexOf(_storage, o);
        return index == -1 ? ResisterObject(o) : index;
    }

    private int FindLastIndex()
    {
        for (var i = 0; i < _storage.Length; i++)
        {
            if (_storage[i] is null)
                return i;
        }

        var nextStorage = new object[_storage.Length + GrowSize];
        Array.Copy(_storage, nextStorage, _storage.Length);
        _storage = nextStorage;
        return _storage.Length;
    }
    public void Inject<T>(T dependency)
    {
    }

}
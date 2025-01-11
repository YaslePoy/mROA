namespace mROA.Implementation;

public class ContextRepository : IContextRepository
{
    private object[] _storage;
    private int _lastIndex;

    private Task<int>? _lastIndexFinder;

    const int StartupSize = 1024;
    const int GrowSize = 128;
    

    public ContextRepository()
    {
        _storage = new object[StartupSize];
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
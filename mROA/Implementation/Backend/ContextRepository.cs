using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using mROA.Abstract;
using mROA.Implementation.Attributes;

namespace mROA.Implementation.Backend
{
    public class ContextRepository : IContextRepository
    {
        private int _debugId = -1;
        private static int LastDebugId = -1;
        // [CanBeNull]
        private Dictionary<int, object?> _singletons;
        private object?[] _storage;

        private Task<int> _lastIndexFinder = Task.FromResult(0);

        private const int StartupSize = 1024;
        private const int GrowSize = 128;


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
                types.ToDictionary(
                    t => t.GetInterfaces().FirstOrDefault(i =>
                        i.GetCustomAttributes(typeof(SharedObjectInterfaceAttribute), true).Length > 0)!.GetHashCode(),
                    Activator.CreateInstance);
        }

        public int ResisterObject(object o, IEndPointContext context)
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

        public T GetObjectBySharedObject<T>(SharedObjectShellShell<T> sharedObjectShellShell)
        {
            return (T)GetObject(sharedObjectShellShell.Identifier.ContextId);
        }

        public object GetObject(int id)
        {
            // Debug.Log($"Reading object {id} from repository with debug ID {_debugId}");
            return (id == -1 || _storage.Length <= id ? null : _storage[id]) ?? throw new NullReferenceException();
        }

        public T GetObject<T>(int id)
        {
            return id == -1 || _storage.Length <= id ? throw new NullReferenceException("Cannot find that object. It is null"): (T)_storage[id]!;
        }

        public object GetSingleObject(Type type)
        {
            return _singletons.GetValueOrDefault(type.GetHashCode()) ?? throw new ArgumentException("Unregistered singleton type");
        }

        public int GetObjectIndex(object o, IEndPointContext context)
        {
            var index = Array.IndexOf(_storage, o);
            return index == -1 ? ResisterObject(o, context) : index;
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
}
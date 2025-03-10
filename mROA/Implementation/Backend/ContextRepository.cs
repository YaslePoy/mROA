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
        private const int StartupSize = 1024;
        private const int GrowSize = 128;
        public static object[] EventBinders = new object[] { };

        private static int LastDebugId = -1;
        private int _debugId = -1;

        private Task<int> _lastIndexFinder = Task.FromResult(0);

        private IRepresentationModuleProducer? _representationModuleProducer;

        // [CanBeNull]
        private Dictionary<int, object?> _singletons;
        private object?[] _storage;


        public ContextRepository()
        {
            _storage = new object[StartupSize];
        }

        public int HostId { get; set; }

        public int ResisterObject<T>(object o, IEndPointContext context)
        {
            if (!_lastIndexFinder.IsCompleted)
                _lastIndexFinder.Wait();

            _storage[_lastIndexFinder.Result] = o;


            var last = _lastIndexFinder.Result;
            _lastIndexFinder = Task.Run(FindLastIndex);
            EventBinders.OfType<IEventBinder<T>>().FirstOrDefault()
                ?.BindEvents((T)o, context, _representationModuleProducer!, last);

            return last;
        }

        public void ClearObject(ComplexObjectIdentifier id)
        {
            _storage[id.ContextId] = null;
            _lastIndexFinder = Task.FromResult(id.ContextId);
        }

        public T GetObjectByShell<T>(SharedObjectShellShell<T> sharedObjectShellShell)
        {
            return (T)GetObject(sharedObjectShellShell.Identifier.ContextId);
        }

        public T? GetObject<T>(ComplexObjectIdentifier id)
        {
            return id.ContextId == -1 || _storage.Length <= id.ContextId
                ? throw new NullReferenceException("Cannot find that object. It is null")
                : (T)_storage[id.ContextId]!;
        }

        public object GetSingleObject(Type type)
        {
            return _singletons.GetValueOrDefault(type.GetHashCode()) ??
                   throw new ArgumentException("Unregistered singleton type");
        }

        public int GetObjectIndex<T>(object o, IEndPointContext context)
        {
            var index = Array.IndexOf(_storage, o);
            return index == -1 ? ResisterObject<T>(o, context) : index;
        }

        public void Inject<T>(T dependency)
        {
            if (dependency is IRepresentationModuleProducer moduleProducer)
            {
                _representationModuleProducer = moduleProducer;
            }
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

        public object GetObject(int id)
        {
            // Debug.Log($"Reading object {id} from repository with debug ID {_debugId}");
            return (id == -1 || _storage.Length <= id ? null : _storage[id]) ?? throw new NullReferenceException();
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
    }
}
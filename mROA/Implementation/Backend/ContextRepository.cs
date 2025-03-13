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
        public static object[] EventBinders = { };

        private static int LastDebugId = -1;
        private int _debugId = -1;

        private Task<int> _lastIndexFinder = Task.FromResult(0);

        private IRepresentationModuleProducer? _representationModuleProducer;

        // [CanBeNull]
        private Dictionary<int, object?> _singletons;
        private IStorage<object> _storage;


        public ContextRepository()
        {
            _storage = new ExtensibleStorage<object>();
        }

        public int HostId { get; set; }

        public int ResisterObject<T>(object o, IEndPointContext context)
        {
            var last = _storage.Place(o);

            EventBinders.OfType<IEventBinder<T>>().FirstOrDefault()
                ?.BindEvents((T)o, context, _representationModuleProducer!, last);

            return last;
        }

        public void ClearObject(ComplexObjectIdentifier id)
        {
            _storage.Free(id.ContextId);
        }

        public T GetObject<T>(ComplexObjectIdentifier id)
        {
            var value = _storage.GetValue(id.ContextId);

            if (value == null)
            {
                throw new NullReferenceException("Cannot find that object. It is null");
            }

            return (T)value;
        }

        public object GetSingleObject(Type type, int ownerId)
        {
            return _singletons.GetValueOrDefault(type.GetHashCode()) ??
                   throw new ArgumentException("Unregistered singleton type");
        }

        public int GetObjectIndex<T>(object o, IEndPointContext context)
        {
            var index = _storage.GetIndex(o);
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
    }
}
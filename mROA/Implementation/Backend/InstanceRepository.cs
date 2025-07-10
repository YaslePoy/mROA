using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using mROA.Abstract;
using mROA.Implementation.Attributes;

namespace mROA.Implementation.Backend
{
    public class InstanceRepository : IInstanceRepository
    {
        public static object[] EventBinders = { };

        private static int LastDebugId = -1;
        private int _debugId = -1;

        private IRepresentationModuleProducer? _representationModuleProducer;

        // [CanBeNull]
        private Dictionary<int, object?> _singletons;
        private IStorage<object> _storage;


        public InstanceRepository()
        {
            _storage = new ExtensibleStorage<object>();
        }

        public int HostId { get; set; }

        public int ResisterObject<T>(object o, IEndPointContext context)
        {
            var last = _storage.Place(o);

            var sharedType = typeof(IShared);
            var interfaces = o.GetType().GetInterfaces();
            var generic = interfaces.Where(i => sharedType.IsAssignableFrom(i) && i != sharedType)
                .Select(i => typeof(IEventBinder<>).MakeGenericType(i));
            
            var binders = EventBinders.Where(i => generic.Any(g => g.IsAssignableFrom(i.GetType())));
            foreach (var binder in binders)
                ((IEventBinder)binder).BindEvents(o, context, _representationModuleProducer!, last);

            return last;
        }

        public void ClearObject(ComplexObjectIdentifier id, IEndPointContext context)
        {
            _storage.Free(id.ContextId);
        }

        public T GetObject<T>(ComplexObjectIdentifier id, IEndPointContext context)  where T : class
        {
            var value = _storage.GetValue(id.ContextId);

            if (value == null)
            {
                throw new NullReferenceException("Cannot find that object. It is null");
            }

            return (T)value;
        }

        public T GetSingletonObject<T>(IEndPointContext context) where T : class, IShared
        {
            return GetSingletonObject(typeof(T), context) as T ??
                   throw new ArgumentException("Unregistered singleton type");
        }

        public object GetSingletonObject(Type type, IEndPointContext context)
        {
            return _singletons.GetValueOrDefault(type.GetHashCode()) ??
                   throw new ArgumentException("Unregistered singleton type");
        }

        public int GetObjectIndex<T>(object o, IEndPointContext context)
        {
            var index = _storage.GetIndex(o);
            return index == -1 ? ResisterObject<T>(o, context) : index;
        }

        public void Inject(object dependency)
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
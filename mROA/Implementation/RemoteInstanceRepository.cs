using System;
using System.Collections.Generic;
using System.Linq;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class RemoteInstanceRepository : IInstanceRepository
    {
        private List<RemoteObjectBase> _producedProxys = new();

        public static Dictionary<Type, Func<int, IRepresentationModule, IEndPointContext, RemoteObjectBase>>
            RemoteTypeFactories = new();

        private IRepresentationModuleProducer? _representationProducer;

        public int HostId { get; set; }

        public int ResisterObject<T>(object o, IEndPointContext context)
        {
            throw new NotSupportedException();
        }

        public void ClearObject(ComplexObjectIdentifier id, IEndPointContext context)
        {
            throw new NotSupportedException();
        }

        public T GetObject<T>(ComplexObjectIdentifier id, IEndPointContext context) where T : class
        {
            var index = _producedProxys.Find(i => i.Identifier.Equals(id));
            if (index is not null)
                return (T)(index as object);
            if (_representationProducer == null)
                throw new NullReferenceException("representation producer is not initialized");

            if (!RemoteTypeFactories.TryGetValue(typeof(T), out var remoteType)) throw new NotSupportedException();
            var representationModule =
                _representationProducer.Produce(context.OwnerId);
            var remote = remoteType(id.ContextId,
                representationModule, context);

            _producedProxys.Add(remote!);

            return (remote as T)!;
        }

        public T GetSingletonObject<T>(IEndPointContext context) where T : class, IShared
        {
            return (GetSingletonObject(typeof(T), context) as T)!;
        }

        public object GetSingletonObject(Type type, IEndPointContext context)
        {
            if (_representationProducer == null)
                throw new NullReferenceException("representation producer is not initialized");

            var representationModule =
                _representationProducer.Produce(context.OwnerId);

            var instance = RemoteTypeFactories[type](-1, representationModule, context)!;

            var remoteObjectBase = instance;

            _producedProxys.Add(remoteObjectBase);

            return _producedProxys.Last();
        }

        public int GetObjectIndex<T>(object o, IEndPointContext context)
        {
            if (o is RemoteObjectBase remote)
            {
                return remote.Id;
            }

            throw new NotSupportedException();
        }

        public void Inject<T>(T dependency)
        {
            if (dependency is IRepresentationModuleProducer serialisationModule)
                _representationProducer = serialisationModule;
        }
    }
}
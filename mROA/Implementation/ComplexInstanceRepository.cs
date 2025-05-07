using System;
using System.Collections.Generic;
using System.Linq;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class ComplexInstanceRepository : IInstanceRepository
    {
        private List<KeyValuePair<int, ExtensibleStorage<object>>> _storages = new();
        public static object[] EventBinders = { };

        private IRemoteObjectFactory? _remoteObjectFactory;
        private IRepresentationModuleProducer? _representationModuleProducer;

        public void Inject<T>(T dependency)
        {
            if (dependency is IRemoteObjectFactory remoteObjectFactory)
            {
                _remoteObjectFactory = remoteObjectFactory;
            }

            if (dependency is IRepresentationModuleProducer moduleProducer)
            {
                _representationModuleProducer = moduleProducer;
            }
        }

        public int HostId { get; set; }

        public int ResisterObject<T>(object o, IEndPointContext context)
        {
            var storageIndex = _storages.FindIndex(i => i.Key == context.OwnerId);
            if (storageIndex == -1)
            {
                _storages.Add(
                    new KeyValuePair<int, ExtensibleStorage<object>>(context.OwnerId, new ExtensibleStorage<object>()));
                storageIndex = _storages.Count - 1;
            }

            var storage = _storages[storageIndex].Value;

            var placedIndex = storage.Place(o);
            EventBinders.OfType<IEventBinder<T>>().FirstOrDefault()
                ?.BindEvents((T)o, context, _representationModuleProducer!, placedIndex);

            return placedIndex;
        }

        public void ClearObject(ComplexObjectIdentifier id, IEndPointContext context)
        {
            _storages.Find(i => i.Key == id.OwnerId).Value.Free(id.ContextId);
        }

        public T GetObject<T>(ComplexObjectIdentifier id, IEndPointContext context)
        {
            throw new NotImplementedException();
        }

        public object GetSingleObject(Type type, IEndPointContext context)
        {
            throw new NotImplementedException();
        }

        public int GetObjectIndex<T>(object o, IEndPointContext context)
        {
            throw new NotImplementedException();
        }
    }
}
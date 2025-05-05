using System;
using System.Collections.Generic;
using System.Linq;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class RemoteContextRepository : IContextRepository
    {
        private List<RemoteObjectBase> _producedRemoteEndpoints = new();
        public static Dictionary<Type, Type> RemoteTypes = new();
        private IRepresentationModuleProducer? _representationProducer;

        public int HostId { get; set; }

        public int ResisterObject<T>(object o, IEndPointContext context)
        {
            throw new NotSupportedException();
        }

        public void ClearObject(ComplexObjectIdentifier id)
        {
            throw new NotSupportedException();
        }

        public T GetObject<T>(ComplexObjectIdentifier id, IEndPointContext context)
        {
            var index = _producedRemoteEndpoints.Find(i => i.Identifier.Equals(id));
            if (index is not null)
                return (T)(index as object);
            if (_representationProducer == null)
                throw new NullReferenceException("representation producer is not initialized");

            if (!RemoteTypes.TryGetValue(typeof(T), out var remoteType)) throw new NotSupportedException();
            var representationModule =
                _representationProducer.Produce(TransmissionConfig.OwnershipRepository.GetOwnershipId());
            var remote = (T)Activator.CreateInstance(remoteType, id.ContextId,
                representationModule, context)!;

            _producedRemoteEndpoints.Add((remote as RemoteObjectBase)!);

            return remote;
        }

        public object GetSingleObject(Type type, int ownerId)
        {
            if (_representationProducer == null)
                throw new NullReferenceException("representation producer is not initialized");

            var representationModule =
                _representationProducer.Produce(ownerId);

            _producedRemoteEndpoints.Add((Activator.CreateInstance(RemoteTypes[type], -1,
                representationModule) as RemoteObjectBase)!);

            return _producedRemoteEndpoints.Last();
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
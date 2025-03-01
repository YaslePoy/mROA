using System;
using System.Collections.Generic;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class RemoteContextRepository : IContextRepository
    {
        private IRepresentationModuleProducer? _representationProducer;
        public static Dictionary<Type, Type> RemoteTypes = new();

        public int ResisterObject(object o)
        {
            throw new NotSupportedException();
        }

        public void ClearObject(int id)
        {
            throw new NotSupportedException();
        }

        public T GetObjectBySharedObject<T>(SharedObjectShellShell<T> sharedObjectShellShell)
        {
            if (_representationProducer == null)
                throw new NullReferenceException("representation producer is not initialized");

            if (!RemoteTypes.TryGetValue(typeof(T), out var remoteType)) throw new NotSupportedException();
            var representationModule = _representationProducer.Produce(sharedObjectShellShell.Identifier.OwnerId);
            var remote = (T)Activator.CreateInstance(remoteType, sharedObjectShellShell.Identifier.ContextId,
                representationModule)!;
            return remote;
        }

        public object GetObject(int id)
        {
            throw new NotSupportedException();
        }

        public T GetObject<T>(int id)
        {
            if (_representationProducer == null)
                throw new NullReferenceException("representation producer is not initialized");

            if (!RemoteTypes.TryGetValue(typeof(T), out var remoteType)) throw new NotSupportedException();
            var representationModule = _representationProducer.Produce(TransmissionConfig.OwnershipRepository.GetOwnershipId());
            var remote = (T)Activator.CreateInstance(remoteType, id,
                representationModule)!;
            return remote;
        }

        public object GetSingleObject(Type type)
        {
            if (_representationProducer == null)
                throw new NullReferenceException("representation producer is not initialized");

            var representationModule = _representationProducer.Produce(TransmissionConfig.OwnershipRepository.GetOwnershipId());
            return Activator.CreateInstance(RemoteTypes[type], -1,
                representationModule)!;
        }

        public int GetObjectIndex(object o)
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
using System;
using System.Collections.Generic;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class RemoteObjectFactory : IRemoteObjectFactory
    {
        public static Dictionary<Type, Type> RemoteTypes = new();
        private IRepresentationModuleProducer? _representationProducer;

        public T Produce<T>(ComplexObjectIdentifier id, IEndPointContext context)
        {
            if (_representationProducer == null)
                throw new NullReferenceException("representation producer is not initialized");

            if (!RemoteTypes.TryGetValue(typeof(T), out var remoteType)) throw new NotSupportedException();
            var representationModule =
                _representationProducer.Produce(context.OwnerId);
            var remote = (T)Activator.CreateInstance(remoteType, id.ContextId,
                representationModule)!;
            return remote;
        }

        public void Inject<T>(T dependency)
        {
            if (dependency is IRepresentationModuleProducer serialisationModule)
                _representationProducer = serialisationModule;
        }
    }
}
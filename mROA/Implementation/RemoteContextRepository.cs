﻿using System.Collections.Frozen;
using mROA.Abstract;

namespace mROA.Implementation;

public class RemoteContextRepository : IContextRepository
{
    private IRepresentationModuleProducer _representationProducer;
    public static FrozenDictionary<Type, Type> RemoteTypes;
    public int ResisterObject(object o)
    {
        throw new NotSupportedException();
    }

    public void ClearObject(int id)
    {
        throw new NotSupportedException();
    }

    public object GetObject(int id)
    {
        throw new NotSupportedException();
    }

    public T GetObject<T>(int id)
    {
        if (RemoteTypes.TryGetValue(typeof(T), out var remoteType))
        {
            var remote = (T)Activator.CreateInstance(remoteType, id, _representationProducer.Produce(TransmissionConfig.OwnershipRepository!.GetOwnershipId()))!;
            return remote;
        }
        throw new NotSupportedException();
    }

    public object GetSingleObject(Type type)
    {
        return Activator.CreateInstance(RemoteTypes[type], -1, _representationProducer.Produce(TransmissionConfig.OwnershipRepository.GetOwnershipId()))!;
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
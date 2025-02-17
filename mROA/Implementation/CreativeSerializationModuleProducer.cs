﻿using mROA.Abstract;
using mROA.Implementation.Backend;
using mROA.Implementation.Frontend;

namespace mROA.Implementation;

public class CreativeSerializationModuleProducer : IRepresentationModuleProducer
{
    private Type _reprModuleType;
    private IInjectableModule[] _creationModules;
    private IConnectionHub? _hub;

    public CreativeSerializationModuleProducer(IInjectableModule[] creationModules, Type reprModuleType)
    {
        _creationModules = creationModules;
        _reprModuleType = reprModuleType;
    }


    public void Inject<T>(T dependency)
    {
        if (dependency is IConnectionHub interactionModule)
            _hub = interactionModule;
    }

    public IRepresentationModule Produce(int id)
    {
        if (_hub == null)
            throw new NullReferenceException("Interaction module is null");

        var produced =
            Activator.CreateInstance(_reprModuleType) as IRepresentationModule ??
            throw new Exception("Bad serialization module type");

        foreach (var creationModule in _creationModules)
            produced.Inject(creationModule);
        
        produced.Inject(_hub.GetInteracion(id));
        
        return produced;
    }
}
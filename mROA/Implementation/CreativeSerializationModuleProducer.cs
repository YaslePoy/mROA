using mROA.Abstract;
using mROA.Implementation.Backend;
using mROA.Implementation.Frontend;

namespace mROA.Implementation;

public class CreativeSerializationModuleProducer : ISerialisationModuleProducer
{
    private Type _serializationModuleType;
    private IInjectableModule[] _creationModules;
    private StreamBasedInteractionModule? _interactionModule;

    public CreativeSerializationModuleProducer(IInjectableModule[] creationModules, Type serializationModuleType)
    {
        _creationModules = creationModules;
        _serializationModuleType = serializationModuleType;
    }


    public void Inject<T>(T dependency)
    {
        if (dependency is StreamBasedInteractionModule interactionModule)
            _interactionModule = interactionModule;
    }

    public ISerialisationModule.IFrontendSerialisationModule Produce(int ownership)
    {
        if (_interactionModule == null)
            throw new NullReferenceException("Interaction module is null");

        var produced =
            Activator.CreateInstance(_serializationModuleType) as ISerialisationModule.IFrontendSerialisationModule ??
            throw new Exception("Bad serialization module type");

        foreach (var creationModule in _creationModules)
            produced.Inject(creationModule);

        var interactionModule = new StreamBasedFrontendInteractionModule
        {
            ClientId = ownership,
            ServerStream = _interactionModule.GetSource(ownership)
        };
        produced.Inject(interactionModule);
        return produced;
    }
}
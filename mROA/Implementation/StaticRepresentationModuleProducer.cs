using mROA.Abstract;

namespace mROA.Implementation;

public class StaticRepresentationModuleProducer : IRepresentationModuleProducer
{
    private IRepresentationModule? _representationModule;
 
    public IRepresentationModule Produce(int ownership)
    {
        if (_representationModule == null)
            throw new NullReferenceException("The representation module is not initialized.");
        return _representationModule;
    }
    
    public void Inject<T>(T dependency)
    {
        if (dependency is IRepresentationModule serialisationModule)
            _representationModule = serialisationModule;
    }
}
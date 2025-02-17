using mROA.Abstract;

namespace mROA.Implementation;

public class StaticRepresentationModuleProducer : IRepresentationModuleProducer
{
    private IRepresentationModule _reprModule;
 
    public IRepresentationModule Produce(int ownership)
    {
        return _reprModule;
    }
    
    public void Inject<T>(T dependency)
    {
        if (dependency is IRepresentationModule serialisationModule)
            _reprModule = serialisationModule;
    }
}
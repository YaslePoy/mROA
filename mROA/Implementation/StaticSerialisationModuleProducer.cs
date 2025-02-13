using mROA.Abstract;

namespace mROA.Implementation;

public class StaticSerialisationModuleProducer : ISerialisationModuleProducer
{
    private ISerialisationModule.IFrontendSerialisationModule _serialisationModule;
 
    public ISerialisationModule.IFrontendSerialisationModule Produce(int ownership)
    {
        return _serialisationModule;
    }
    
    public void Inject<T>(T dependency)
    {
        if (dependency is ISerialisationModule.IFrontendSerialisationModule serialisationModule)
            _serialisationModule = serialisationModule;
    }
}
namespace mROA.Implementation;

public class FrontendServiceBuilder
{
    protected IInteractionModule.IFrontendInteractionModule _interactionModule;
    protected ISerialisationModule.IFrontendSerialisationModule _serialisationModule;
    
    public ISerialisationModule.IFrontendSerialisationModule Build()
    {
        return _serialisationModule;
    }
}
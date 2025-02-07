using mROA.Abstract;

namespace mROA.Implementation.Frontend;

public class FrontendServiceBuilder
{
    protected IInteractionModule.IFrontendInteractionModule? InteractionModule;
    protected ISerialisationModule.IFrontendSerialisationModule? SerialisationModule;
    
    public ISerialisationModule.IFrontendSerialisationModule Build()
    {
        return SerialisationModule!;
    }
}
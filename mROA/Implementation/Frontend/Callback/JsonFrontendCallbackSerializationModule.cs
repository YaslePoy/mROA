using mROA.Abstract;

namespace mROA.Implementation.Frontend;

public class JsonFrontendCallbackSerializationModule : ISerialisationModule
{
    private IInteractionModule.IFrontendInteractionModule? _interactionModule;

    public void Inject<T>(T dependency)
    {
        if (dependency is IInteractionModule.IFrontendInteractionModule interactionModule)
            _interactionModule = interactionModule;
    }

    public void HandleIncomingRequest(int clientId, byte[] message)
    {
        
    }

    public void PostResponse(NetworkMessage message, int clientId)
    {
        throw new NotImplementedException();
    }

    public void SendWelcomeMessage(int clientId)
    {
        throw new NotImplementedException();
    }
}
namespace mROA.Abstract;

public delegate void ConnectionHandler(INextGenerationInteractionModule interactionModule);
public delegate void DisconnectionHandler(INextGenerationInteractionModule interactionModule);

public interface IConnectionHub
{
    void RegisterInteracion(INextGenerationInteractionModule interaction);
    INextGenerationInteractionModule GetInteracion(int id);
    event ConnectionHandler? OnConnectied;
    event DisconnectionHandler? OnDisconnected;
}
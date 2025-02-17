namespace mROA.Abstract;

public delegate void ConnectionHandler(IRepresentationModule representationModule);
public delegate void DisconnectionHandler(IRepresentationModule representationModule);

public interface IConnectionHub
{
    void RegisterInteraction(INextGenerationInteractionModule interaction);
    INextGenerationInteractionModule GetInteracion(int id);
    event ConnectionHandler? OnConnected;
    event DisconnectionHandler? OnDisconnected;
}
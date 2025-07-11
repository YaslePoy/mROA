namespace mROA.Abstract
{
    public delegate void ConnectionHandler(IRepresentationModule representationModule);

    public delegate void DisconnectionHandler(IRepresentationModule representationModule);

    public interface IConnectionHub
    {
        void RegisterInteraction(IChannelInteractionModule interaction);
        IChannelInteractionModule GetInteraction(int id);
        event ConnectionHandler? OnConnected;
        event DisconnectionHandler? OnDisconnected;
    }
}
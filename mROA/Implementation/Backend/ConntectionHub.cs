using mROA.Abstract;

namespace mROA.Implementation.Backend;

public class ConntectionHub : IConnectionHub
{
    private Dictionary<int, INextGenerationInteractionModule> _connections = new();
    public void RegisterInteracion(INextGenerationInteractionModule interaction)
    {
        
        OnConnectied?.Invoke(interaction);
    }

    public INextGenerationInteractionModule GetInteracion(int id)
    {
        
    }

    public event ConnectionHandler? OnConnectied;
    public event DisconnectionHandler? OnDisconnected;
}
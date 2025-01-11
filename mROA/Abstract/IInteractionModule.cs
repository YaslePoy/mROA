namespace mROA;

public interface IInteractionModule
{
    void SetInteractionHandler(Action<(int clientId, byte[] message)> handler);
    void SendTo(int clientId, byte[] message);
}
namespace mROA;

public interface IInteractionModule
{
    void SetMessageHandler(Action<int, string> handler);
    void SendTo(int clientId, byte[] message);
}
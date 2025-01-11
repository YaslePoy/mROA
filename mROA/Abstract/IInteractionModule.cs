namespace mROA;

public interface IInteractionModule
{
    void SendTo(int clientId, byte[] message);
}
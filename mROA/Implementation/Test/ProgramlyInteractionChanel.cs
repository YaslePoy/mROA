namespace mROA.Implementation;

public class ProgramlyInteractionChanel : IInteractionModule
{
    public void PassCommand(int clientId, byte[] message) => _handler(clientId, message);
    private Action<int, byte[]> _handler;
    
    public List<string> OutputBuffer = new List<string>();
    
    public void SetMessageHandler(Action<int, byte[]> handler)
    {
        _handler = handler;
    }

    public void SendTo(int clientId, byte[] message)
    {
        Console.WriteLine("{0}: {1}", clientId, System.Text.Encoding.UTF8.GetString(message));
        OutputBuffer.Add(System.Text.Encoding.UTF8.GetString(message));
    }

    public void RegisterSourse(Stream stream)
    {
        throw new NotImplementedException();
    }
}
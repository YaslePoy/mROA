namespace mROA.Implementation;

public class ConsoleInteractionChanel(IInputModule inputModule) : IInteractionModule
{
    public void StartListeningKeyboard()
    {
        while (true)
        {
            var input = Console.ReadLine();
            inputModule.HandleIncomingRequest(123, input);
        }
    }
    
    public void SendTo(int clientId, byte[] message)
    {
        Console.WriteLine("{0}: {1}", clientId, System.Text.Encoding.UTF8.GetString(message));
    }
}
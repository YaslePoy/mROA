using System.Text;

namespace mROA.Implementation;

public class ConsoleFrontendInteractionModule : IInteractionModule.IFrontendInteractionModule
{
    public byte[] ReceiveMessage()
    {
        
    }

    public void PostMessage(byte[] message)
    {
        Console.WriteLine(Encoding.UTF8.GetString(message));
    }
}
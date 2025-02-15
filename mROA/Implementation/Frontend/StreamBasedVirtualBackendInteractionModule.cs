using mROA.Implementation.Backend;

namespace mROA.Implementation.Frontend;

public class StreamBasedVirtualBackendInteractionModule : StreamBasedInteractionModule
{
    public Stream ServerStream { get; set; }

    public void RegisterSource(Stream stream)
    {
        ServerStream = stream;
    }
    
    public Stream GetSource(int clientId)
    {
        return ServerStream;
    }
    
    public void SendTo(int clientId, byte[] message)
    {
        ServerStream.Write(BitConverter.GetBytes((ushort)message.Length), 0, sizeof(ushort));
        ServerStream.Write(message, 0, message.Length);
    }

    private async Task ListenTo((int id, Stream stream) client, Action<int, byte[]> action)
    {
        const int bufferSize = ushort.MaxValue;
        try
        {
            byte[] buffer = new byte[bufferSize];
            while (client.stream.CanRead)
            {
                await client.stream.ReadExactlyAsync(buffer, 0, 2);
                var len = BitConverter.ToUInt16(buffer, 0);
                await client.stream.ReadExactlyAsync(buffer, 0, len);
                _ = Task.Run(() => action(client.id, buffer[..len]));
            }
        }
        catch (Exception)
        {
        }
    }
    
}
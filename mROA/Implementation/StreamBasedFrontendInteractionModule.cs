namespace mROA.Implementation;

public class StreamBasedFrontendInteractionModule : IInteractionModule.IFrontendInteractionModule
{
    public Stream ServerStream { get; set; }

    public async Task<byte[]> ReceiveMessage()
    {
        const int bufferSize = ushort.MaxValue;

        byte[] buffer = new byte[bufferSize];
        if (!ServerStream.CanRead) throw new IOException("Server is not connected.");

        await ServerStream.ReadExactlyAsync(buffer, 0, 2);
        var len = BitConverter.ToUInt16(buffer, 0);
        await ServerStream.ReadExactlyAsync(buffer, 0, len);

        return buffer[..len];
    }

    public void PostMessage(byte[] message)
    {
        ServerStream.Write(BitConverter.GetBytes((ushort)message.Length), 0, sizeof(ushort));
        ServerStream.Write(message, 0, message.Length);
    }
}
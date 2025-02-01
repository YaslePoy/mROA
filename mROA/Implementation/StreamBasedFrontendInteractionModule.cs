namespace mROA.Implementation;

public class StreamBasedFrontendInteractionModule : IInteractionModule.IFrontendInteractionModule
{
    private Stream _serverStream;

    public StreamBasedFrontendInteractionModule(Stream serverStream)
    {
        _serverStream = serverStream;
    }

    public byte[] ReceiveMessage()
    {
        const int bufferSize = ushort.MaxValue;

            byte[] buffer = new byte[bufferSize];
            if (!_serverStream.CanRead) throw new IOException("Server is not connected.");
            
            _serverStream.ReadExactly(buffer, 0, 2);
            var len = BitConverter.ToUInt16(buffer, 0);
            _serverStream.ReadExactly(buffer, 0, len);

            return buffer[..len];
    }

    public void PostMessage(byte[] message)
    {
        _serverStream.Write(BitConverter.GetBytes((ushort)message.Length), 0, sizeof(ushort));
        _serverStream.Write(message, 0, message.Length);
    }
}
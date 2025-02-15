namespace mROA.Implementation;

public class AsyncNetworkInteractionModule
{
    private ISerializationToolkit _iSerializationToolkit;
    private const int BufferSize = ushort.MaxValue;
    private Stream? _stream;
    private List<NetworkMessage> _unhandledMessages = new(8);
    private byte[] _buffer = new byte[BufferSize];
    private Task? _currentReadTask;
    
    
    public Task ReceiveNext()
    {
        _currentReadTask ??= StartReceiving();
        return _currentReadTask;
    }

    private async Task StartReceiving()
    {
        await _stream.ReadExactlyAsync(_buffer, 0, 2);
        var len = BitConverter.ToUInt16(_buffer, 0);
        await _stream.ReadExactlyAsync(_buffer, 0, len);
        
    }
    
    public NetworkMessage GetLastMessage()
    {
        return _unhandledMessages.Last();
    }
    
    
}
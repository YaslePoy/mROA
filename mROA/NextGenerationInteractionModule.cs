using mROA.Abstract;
using mROA.Implementation;

namespace mROA;

public class NextGenerationInteractionModule : INextGenerationInteractionModule
{
    private ISerializationToolkit _serialization;
    public int ConntectionId { get; set; }
    public Stream BaseStream { get; set; }
    private Task<NetworkMessage>? _currentReceiving;
    private const int BufferSize = ushort.MaxValue;
    private byte[] _buffer = new byte[BufferSize];
    private List<NetworkMessage> _unhandledMessages = new(8);

    public void Inject<T>(T dependency)
    {
        if (dependency is ISerializationToolkit toolkit)
            _serialization = toolkit;
    }


    public Task<NetworkMessage> GetNextMessageReceiving()
    {
        if (_currentReceiving is { IsCompleted: false })
            return _currentReceiving;

        _currentReceiving = GetNextMessage();
        return _currentReceiving;
    }

    public void PostMessage(NetworkMessage message)
    {
        var rawMessage = _serialization.Serialize(message);
        BaseStream.Write(BitConverter.GetBytes((ushort)rawMessage.Length), 0, sizeof(ushort));
        BaseStream.Write(rawMessage, 0, rawMessage.Length);
    }

    public NetworkMessage[] UnhandledMessages => _unhandledMessages.ToArray();

    public void HandleMessage(NetworkMessage msg)
    {
        _unhandledMessages.Remove(msg);
    }

    private async Task<NetworkMessage> GetNextMessage()
    {
        await BaseStream.ReadExactlyAsync(_buffer, 0, 2);
        var len = BitConverter.ToUInt16(_buffer, 0);
        await BaseStream.ReadExactlyAsync(_buffer, 0, len);
        
        return _serialization.Deserialize<NetworkMessage>(_buffer[..len])!;
    }
}
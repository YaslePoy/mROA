using System.Security.Cryptography;
using mROA.Abstract;

namespace mROA.Implementation;

public class NextGenerationInteractionModule : INextGenerationInteractionModule
{
    private ISerializationToolkit? _serialization;
    public int ConnectionId { get; private set; }
    public Stream? BaseStream { get; set; }
    private Task<NetworkMessage>? _currentReceiving;
    private const int BufferSize = ushort.MaxValue;
    private readonly byte[] _buffer = new byte[BufferSize];

    public void Inject<T>(T dependency)
    {
        switch (dependency)
        {
            case ISerializationToolkit toolkit:
                _serialization = toolkit;
                break;
            case IIdentityGenerator identityGenerator:
                ConnectionId = identityGenerator.GetNextIdentity();
                break;
        }
    }


    public Task<NetworkMessage> GetNextMessageReceiving()
    {
        if (_currentReceiving is { IsCompleted: false })
            return _currentReceiving;

        _currentReceiving = GetNextMessage();
        return _currentReceiving;
    }

    public async Task PostMessage(NetworkMessage message)
    {
        if (BaseStream == null)
            throw new NullReferenceException("BaseStream is null");
        
        var rawMessage = _serialization.Serialize(message);
        await BaseStream.WriteAsync(BitConverter.GetBytes((ushort)rawMessage.Length).AsMemory(0, sizeof(ushort)));
        await BaseStream.WriteAsync(rawMessage);
    }

    private async Task<NetworkMessage> GetNextMessage()
    {
        if (BaseStream == null)
            throw new NullReferenceException("BaseStream is null");

        if (_serialization == null)
            throw new NullReferenceException("Serialization toolkit is null");

        await BaseStream.ReadExactlyAsync(_buffer, 0, 2);
        var len = BitConverter.ToUInt16(_buffer, 0);
        await BaseStream.ReadExactlyAsync(_buffer, 0, len);
        
        return _serialization.Deserialize<NetworkMessage>(_buffer[..len])!;
    }
}
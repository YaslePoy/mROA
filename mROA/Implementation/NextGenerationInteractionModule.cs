using mROA.Abstract;

namespace mROA.Implementation;

public class NextGenerationInteractionModule : INextGenerationInteractionModule
{
    private ISerializationToolkit? _serialization;
    public int ConnectionId { get; private set; }
    public Stream BaseStream { get; set; } = Stream.Null;
    private Task<NetworkMessage>? _currentReceiving;
    private const int BufferSize = ushort.MaxValue;
    private readonly Memory<byte> _buffer = new byte[BufferSize];
    private readonly List<NetworkMessage> _messageBuffer = new (128);
    public NetworkMessage[] UnhandledMessages => _messageBuffer.ToArray();
    public NetworkMessage LastMessage { get; private set; } = NetworkMessage.Null;
    public EventWaitHandle CurrentReceivingHandle { get; private set; } = new(true, EventResetMode.ManualReset);

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


    public async void StartInfiniteReceiving()
    {
        try
        {
            CurrentReceivingHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            await Task.Yield();
            while (true)
            {
                var message = ReceiveMessage();
                LastMessage = message;
                _messageBuffer.Add(message);
                CurrentReceivingHandle.Set();
                CurrentReceivingHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }

    
    
    public Task<NetworkMessage> GetNextMessageReceiving()
    {
        if (_currentReceiving != null) return _currentReceiving;
        _currentReceiving = Task.Run(GetNextMessage);
        return _currentReceiving;

    }

    public async Task PostMessage(NetworkMessage message)
    {
        if (BaseStream == null)
            throw new NullReferenceException("BaseStream is null");

        if (_serialization == null)
            throw new NullReferenceException("Serialization toolkit is not initialized");
        
        // Console.WriteLine("Sending {0}", JsonSerializer.Serialize(message));


        var rawMessage = _serialization.Serialize(message);
        await BaseStream.WriteAsync(BitConverter.GetBytes((ushort)rawMessage.Length).AsMemory(0, sizeof(ushort)));
        await BaseStream.WriteAsync(rawMessage);
    }

    public void HandleMessage(NetworkMessage message)
    {
        _messageBuffer.Remove(message);
    }

    public NetworkMessage FirstByFilter(Predicate<NetworkMessage> predicate)
    {
        return _messageBuffer.FirstOrDefault(m => predicate(m)) ?? NetworkMessage.Null;
    }

    private NetworkMessage GetNextMessage()
    {
        if (BaseStream == null)
            throw new NullReferenceException("BaseStream is null");

        if (_serialization == null)
            throw new NullReferenceException("Serialization toolkit is null");
        
        var message = ReceiveMessage();
        
        _messageBuffer.Add(message);
        _currentReceiving = Task.Run(GetNextMessage);
        
        return message;
    }

    private NetworkMessage ReceiveMessage()
    {
        var len = BitConverter.ToUInt16([(byte)BaseStream.ReadByte(), (byte)BaseStream.ReadByte()]);
        var localSpan = _buffer.Span.Slice(0, len);
        BaseStream.ReadExactly(localSpan);
        
        var message = _serialization!.Deserialize<NetworkMessage>(localSpan);
        return message!;
    }
}
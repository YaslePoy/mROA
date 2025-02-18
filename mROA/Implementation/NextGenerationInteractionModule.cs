﻿using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    private List<NetworkMessage> _messageBuffer = [];

    
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
        Console.WriteLine(_currentReceiving?.Status);
        if (_currentReceiving is { Status: TaskStatus.Running })
            return _currentReceiving;

        _currentReceiving = Task.Run(GetNextMessage);
        return _currentReceiving;
    }

    public async Task PostMessage(NetworkMessage message)
    {
        if (BaseStream == null)
            throw new NullReferenceException("BaseStream is null");
        
        Console.WriteLine("Sending {0}", JsonSerializer.Serialize(message));

        
        var rawMessage = _serialization.Serialize(message);
        await BaseStream.WriteAsync(BitConverter.GetBytes((ushort)rawMessage.Length).AsMemory(0, sizeof(ushort)));
        await BaseStream.WriteAsync(rawMessage);
    }

    private NetworkMessage GetNextMessage()
    {
        if (BaseStream == null)
            throw new NullReferenceException("BaseStream is null");

        if (_serialization == null)
            throw new NullReferenceException("Serialization toolkit is null");


        Console.WriteLine("Receiving message");
        BaseStream.ReadExactly(_buffer, 0, 2);
        var len = BitConverter.ToUInt16(_buffer, 0);
        BaseStream.ReadExactly(_buffer, 0, len);

        Console.WriteLine("Receiving {0}", Encoding.Default.GetString(_buffer[..len]));
        
        var message = JsonSerializer.Deserialize<NetworkMessage>(Encoding.Default.GetString(_buffer[..len]));
        _messageBuffer.Add(message!);

        return message!;
    }
}
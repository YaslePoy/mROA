using mROA.Abstract;

namespace mROA.Implementation;

public class RepresentationModule : IRepresentationModule
{
    private ISerializationToolkit? _serialization;
    private INextGenerationInteractionModule? _interaction;

    public void Inject<T>(T dependency)
    {
        switch (dependency)
        {
            case ISerializationToolkit toolkit:
                _serialization = toolkit;
                break;
            case INextGenerationInteractionModule interactionModule:
                _interaction = interactionModule;
                break;
        }
    }

    public int Id => (_interaction ?? throw new NullReferenceException("Interaction is not initialized")).ConnectionId;

    public async Task<T> GetMessageAsync<T>(Guid? requestId, EMessageType? messageType,
        CancellationToken token = default)
    {
        if (_serialization == null)
            throw new NullReferenceException("Serialization toolkit is not initialized");

        var raw = await GetRawMessage(m => m.IsValidMessage(requestId, messageType), token);
        return _serialization.Deserialize<T>(raw)!;
    }

    public T GetMessage<T>(Guid? requestId = null, EMessageType? messageType = null)
    {
        return GetMessage<T>(m => m.IsValidMessage(requestId, messageType));
    }

    public T GetMessage<T>(Predicate<NetworkMessage> filter)
    {
        if (_serialization == null)
            throw new NullReferenceException("Serialization toolkit is not initialized");
        var raw = GetRawMessage(filter).GetAwaiter().GetResult();
        return _serialization.Deserialize<T>(raw)!;
    }

    public async Task<byte[]> GetRawMessage(Predicate<NetworkMessage> filter, CancellationToken token = default)
    {
        await Task.Yield();

        if (_interaction == null)
            throw new NullReferenceException("Interaction toolkit is not initialized");
        
        if (filter(_interaction.LastMessage))
        {
            _interaction.HandleMessage(_interaction.LastMessage);
            return _interaction.LastMessage.Data;
        }

        var message = _interaction.FirstByFilter(filter);

        if (message != NetworkMessage.Null)
        {
            _interaction.HandleMessage(message);
            return message.Data;
        }


        while (!token.IsCancellationRequested)
        {
            var handle = _interaction.CurrentReceivingHandle;
            handle.WaitOne();

            message = _interaction.LastMessage;

            if (!filter(message)) 
                continue;

            message = _interaction.LastMessage;

            _interaction.HandleMessage(message);
            return message.Data;
        }

        return [];
    }

    public async Task PostCallMessageAsync<T>(Guid id, EMessageType eMessageType, T payload) where T : notnull
    {
        await PostCallMessageAsync(id, eMessageType, payload, typeof(T));
    }

    public async Task PostCallMessageAsync(Guid id, EMessageType eMessageType, object payload, Type payloadType)
    {
        if (_interaction == null)
            throw new NullReferenceException("Interaction toolkit is not initialized");
        if (_serialization == null)
            throw new NullReferenceException("Serialization toolkit is not initialized");

        var message = new NetworkMessage
            { Id = id, MessageType = eMessageType, Data = _serialization.Serialize(payload, payloadType) };
        await _interaction.PostMessage(message);
    }

    public void PostCallMessage<T>(Guid id, EMessageType eMessageType, T payload) where T : notnull
    {
        PostCallMessageAsync(id, eMessageType, payload).GetAwaiter().GetResult();
    }

    public void PostCallMessage(Guid id, EMessageType eMessageType, object payload, Type payloadType)
    {
        PostCallMessageAsync(id, eMessageType, payload, payloadType).GetAwaiter().GetResult();
    }
}
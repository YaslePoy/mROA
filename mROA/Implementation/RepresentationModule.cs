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

    public async Task<T> GetMessageAsync<T>(Guid? requestId, MessageType? messageType,
        CancellationToken token = default)
    {
        if (_serialization == null)
            throw new NullReferenceException("Serialization toolkit is not initialized");

        return _serialization.Deserialize<T>(await GetRawMessage(m => m.IsValidMessage(requestId, messageType), token))!;
    }

    public T GetMessage<T>(Guid? requestId = null, MessageType? messageType = null)
    {
        return GetMessage<T>(m => m.IsValidMessage(requestId, messageType));
    }

    public T GetMessage<T>(Predicate<NetworkMessage> filter)
    {
        if (_serialization == null)
            throw new NullReferenceException("Serialization toolkit is not initialized");

        return _serialization.Deserialize<T>(GetRawMessage(filter).GetAwaiter().GetResult())!;
    }

    public async Task<byte[]> GetRawMessage(Predicate<NetworkMessage> filter, CancellationToken token = default)
    {
        await Task.Yield();

        if (_interaction == null)
            throw new NullReferenceException("Interaction toolkit is not initialized");

        // Console.WriteLine(
        //     $"{DateTime.Now.TimeOfDay} {Environment.CurrentManagedThreadId} Representation : Reading");

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
            // Console.WriteLine(
            //     $"{DateTime.Now.TimeOfDay} {Environment.CurrentManagedThreadId} Representation : Receiving message...");
            var handle = _interaction.CurrentReceivingHandle;
            handle.WaitOne();

            message = _interaction.LastMessage;

            // Console.WriteLine(
            //     $"{DateTime.Now.TimeOfDay} {Environment.CurrentManagedThreadId} Representation : Message received {message.SchemaId} - {message.Id}");
            if (!filter(message)) continue;

            message = _interaction.LastMessage;
            // Console.WriteLine(
            //     $"{DateTime.Now.TimeOfDay} {Environment.CurrentManagedThreadId} Representation : Message received Successfully {message.SchemaId} - {message.Id}");
            _interaction.HandleMessage(message);
            return message.Data;
        }

        return [];
    }

    public async Task PostCallMessageAsync<T>(Guid id, MessageType messageType, T payload) where T : notnull
    {
        await PostCallMessageAsync(id, messageType, payload, typeof(T));
    }

    public async Task PostCallMessageAsync(Guid id, MessageType messageType, object payload, Type payloadType)
    {
        if (_interaction == null)
            throw new NullReferenceException("Interaction toolkit is not initialized");
        if (_serialization == null)
            throw new NullReferenceException("Serialization toolkit is not initialized");

        await _interaction.PostMessage(new NetworkMessage
            { Id = id, SchemaId = messageType, Data = _serialization.Serialize(payload, payloadType) });
    }

    public void PostCallMessage<T>(Guid id, MessageType messageType, T payload) where T : notnull
    {
        PostCallMessageAsync(id, messageType, payload).GetAwaiter().GetResult();
    }

    public void PostCallMessage(Guid id, MessageType messageType, object payload, Type payloadType)
    {
        PostCallMessageAsync(id, messageType, payload, payloadType).GetAwaiter().GetResult();
    }
}
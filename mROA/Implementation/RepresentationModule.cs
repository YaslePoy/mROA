using System.Text.Json;
using mROA.Abstract;

namespace mROA.Implementation;

public class RepresentationModule : IRepresentationModule
{
    private ISerializationToolkit _serialization;
    private INextGenerationInteractionModule _interaction;

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

    public int Id => _interaction.ConnectionId;

    public async Task<T> GetMessageAsync<T>(Guid? requestId, MessageType? messageType)
    {
        return _serialization.Deserialize<T>(await GetRawMessage(requestId, messageType))!;
    }

    public T GetMessage<T>(Guid? requestId = null, MessageType? messageType = null)
    {
        return _serialization.Deserialize<T>(GetRawMessage(requestId, messageType).GetAwaiter().GetResult())!;
    }

    public async Task<byte[]> GetRawMessage(Guid? requestId = null, MessageType? messageType = null)
    {
        while (true)
        {
            var message = await _interaction.GetNextMessageReceiving();
            if ((requestId is null || message.Id == requestId) &&
                (messageType is null || message.SchemaId == messageType))
                return message.Data;
        }
    }

    public async Task PostCallMessageAsync<T>(Guid id, MessageType messageType, T payload)
    {
        await PostCallMessageAsync(id, messageType, payload, typeof(T));
    }

    public async Task PostCallMessageAsync(Guid id, MessageType messageType, object payload, Type payloadType)
    {
        await _interaction.PostMessage(new NetworkMessage
            { Id = id, SchemaId = messageType, Data = _serialization.Serialize(payload, payloadType) });
    }

    public void PostCallMessage<T>(Guid id, MessageType messageType, T payload)
    {
        PostCallMessageAsync(id, messageType, payload).GetAwaiter().GetResult();
    }

    public void PostCallMessage(Guid id, MessageType messageType, object payload, Type payloadType)
    {
        PostCallMessageAsync(id, messageType, payload, payloadType).GetAwaiter().GetResult();
    }
}
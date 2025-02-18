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

    public async Task<T> GetMessage<T>(Guid? requestId, MessageType? messageType)
    {
        return _serialization.Deserialize<T>(await GetRawMessage(requestId, messageType))!;
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

    public async Task PostCallMessage<T>(Guid id, MessageType messageType, T payload)
    {
        await PostCallMessage(id, messageType, payload, typeof(T));
    }

    public async Task PostCallMessage(Guid id, MessageType messageType, object payload, Type payloadType)
    {
        await _interaction.PostMessage(new NetworkMessage
            { Id = id, SchemaId = messageType, Data = _serialization.Serialize(payload, payloadType) });
    }
}
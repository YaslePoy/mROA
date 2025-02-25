using System;
using System.Threading;
using System.Threading.Tasks;
using mROA.Abstract;

namespace mROA.Implementation
{
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

        public int Id => (_interaction ?? throw new NullReferenceException("Interaction is not initialized"))
            .ConnectionId;

        public async Task<T> GetMessageAsync<T>(Guid? requestId, MessageType? messageType,
            CancellationToken token = default)
        {
            if (_serialization == null)
                throw new NullReferenceException("Serialization toolkit is not initialized");

            var rawMessage = await GetRawMessage(requestId, messageType, token);
            return _serialization.Deserialize<T>(rawMessage)!;
        }

        public T GetMessage<T>(Guid? requestId = null, MessageType? messageType = null)
        {
            if (_serialization == null)
                throw new NullReferenceException("Serialization toolkit is not initialized");

            var rawMessage = GetRawMessage(requestId, messageType).GetAwaiter().GetResult();
            return _serialization.Deserialize<T>(rawMessage)!;
        }

        public async Task<byte[]> GetRawMessage(Guid? requestId = null, MessageType? messageType = null,
            CancellationToken token = default)
        {
            if (_interaction == null)
                throw new NullReferenceException("Interaction toolkit is not initialized");

            var fromBuffer =
                _interaction.FirstByFilter(message =>
                    (requestId is null || message.Id == requestId) &&
                    (messageType is null || message.SchemaId == messageType));

            if (fromBuffer == null)
            {
                while (token.IsCancellationRequested == false)
                {
                    var message = await _interaction.GetNextMessageReceiving();
                    if ((requestId is not null && message.Id != requestId) ||
                        (messageType is not null && message.SchemaId != messageType))
                        continue;

                    _interaction.HandleMessage(message);
                    return message.Data;
                }
            }

            _interaction.HandleMessage(fromBuffer);
            return fromBuffer.Data;
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
#if TRACE
            Console.WriteLine($"Posting message: {id} - {messageType}");
#endif

            var serialized = _serialization.Serialize(payload, payloadType);
            await _interaction.PostMessage(new NetworkMessage
                { Id = id, SchemaId = messageType, Data = serialized });
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
}
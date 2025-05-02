using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class RepresentationModule : IRepresentationModule
    {
        private IChannelInteractionModule? _interaction;
        private ISerializationToolkit? _serialization;

        public void Inject<T>(T dependency)
        {
            switch (dependency)
            {
                case ISerializationToolkit toolkit:
                    _serialization = toolkit;
                    break;
                case IChannelInteractionModule interactionModule:
                    _interaction = interactionModule;
                    break;
            }
        }

        
        
        public int Id => (_interaction ?? throw new NullReferenceException("Interaction is not initialized"))
            .ConnectionId;

        public Task<(object parced, EMessageType originalType)> GetSingle(Predicate<NetworkMessageHeader> rule, CancellationToken token, params Func<NetworkMessageHeader, Type?>[] converter)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<(object parced, EMessageType originalType)> GetStream(Predicate<NetworkMessageHeader> rule, CancellationToken token, params Func<NetworkMessageHeader, Type?>[] converter)
        {
            throw new NotImplementedException();
        }

        public async Task<T> GetMessageAsync<T>(Guid? requestId, EMessageType? messageType,
            CancellationToken token = default)
        {
            if (_serialization == null)
                throw new NullReferenceException("Serialization toolkit is not initialized");

            var rawMessage = await GetRawMessage(requestId, messageType, token);
            return _serialization.Deserialize<T>(rawMessage)!;
        }

        public T GetMessage<T>(Guid? requestId = null, EMessageType? messageType = null)
        {
            if (_serialization == null)
                throw new NullReferenceException("Serialization toolkit is not initialized");

            var rawMessage = GetRawMessage(requestId, messageType).GetAwaiter().GetResult();
            return _serialization.Deserialize<T>(rawMessage)!;
        }

        public async Task<byte[]> GetRawMessage(Guid? requestId = null, EMessageType? messageType = null,
            CancellationToken token = default)
        {
            if (_interaction == null)
                throw new NullReferenceException("Interaction toolkit is not initialized");

            var fromBuffer =
                _interaction.FirstByFilter(message =>
                    (requestId is null || message.Id == requestId) &&
                    (messageType is null || message.MessageType == messageType));

            if (fromBuffer == null)
            {
                while (token.IsCancellationRequested == false)
                {
                    var message = await _interaction.GetNextMessageReceiving();
                    if ((requestId is not null && message.Id != requestId) ||
                        (messageType is not null && message.MessageType != messageType))
                        continue;

                    _interaction.HandleMessage(message);
                    return message.Data;
                }
            }

            if (fromBuffer == null)
            {
                return Array.Empty<byte>();
            }

            _interaction.HandleMessage(fromBuffer);
            return fromBuffer.Data;
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

            var serialized = _serialization.Serialize(payload, payloadType);
            await _interaction.PostMessageAsync(new NetworkMessageHeader
                { Id = id, MessageType = eMessageType, Data = serialized });
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
}
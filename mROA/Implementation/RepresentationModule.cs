using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class RepresentationModule : IRepresentationModule
    {
        private IChannelInteractionModule? _interaction;
        private IContextualSerializationToolKit? _serialization;

        public void Inject<T>(T dependency)
        {
            switch (dependency)
            {
                case IContextualSerializationToolKit toolkit:
                    _serialization = toolkit;
                    break;
                case IChannelInteractionModule interactionModule:
                    _interaction = interactionModule;
                    break;
            }
        }


        public int Id => (_interaction ?? throw new NullReferenceException("Interaction is not initialized"))
            .ConnectionId;

        public async Task<(object? Deserialized, EMessageType MessageType)> GetSingle(
            Predicate<NetworkMessageHeader> rule, IEndPointContext? context,
            CancellationToken token = default, params Func<NetworkMessageHeader, Type?>[] converter)
        {
            var writer = _interaction.ReceiveChanel.Writer;
            var reader = _interaction.ReceiveChanel.Reader;


            await foreach (var message in reader.ReadAllAsync(token))
            {
                if (!rule(message))
                {
                    await writer.WriteAsync(message, token);
                    continue;
                }

                var type = converter.Select(i => i(message)).First(i => i != null)!;
                var deserialized = _serialization.Deserialize(message.Data, type, context);
                return (deserialized, message.MessageType);
            }

            return (null, EMessageType.Unknown);
        }

        public async IAsyncEnumerable<(object parced, EMessageType originalType)> GetStream(
            Predicate<NetworkMessageHeader> rule, IEndPointContext? context, [EnumeratorCancellation] CancellationToken token = default,
            params Func<NetworkMessageHeader, Type?>[] converter)
        {
            var writer = _interaction?.ReceiveChanel.Writer;
            await foreach (var message in _interaction.ReceiveChanel.Reader.ReadAllAsync(token))
            {
                if (!rule(message))
                {
                    await writer.WriteAsync(message, token);
                    continue;
                }

                var type = converter.Select(i => i(message)).First(i => i != null)!;
                var deserialized = _serialization.Deserialize(message.Data, type, context);
                yield return (deserialized, message.MessageType)!;
            }
        }

        public async Task PostCallMessageAsync<T>(Guid id, EMessageType eMessageType, T payload,
            IEndPointContext? context) where T : notnull
        {
            await PostCallMessageAsync(id, eMessageType, payload, context);
        }
        
        public async Task PostCallMessageAsync(Guid id, EMessageType eMessageType, object payload,
            IEndPointContext? context)
        {
            if (_interaction == null)
                throw new NullReferenceException("Interaction toolkit is not initialized");
            if (_serialization == null)
                throw new NullReferenceException("Serialization toolkit is not initialized");

            var serialized = _serialization.Serialize(payload, context);
            await _interaction.PostMessageAsync(new NetworkMessageHeader
                { Id = id, MessageType = eMessageType, Data = serialized });
        }

        public void PostCallMessage<T>(Guid id, EMessageType eMessageType, T payload, IEndPointContext? context) where T : notnull
        {
            PostCallMessageAsync(id, eMessageType, payload, context).GetAwaiter().GetResult();
        }

        public async Task PostCallMessageUntrustedAsync<T>(Guid id, EMessageType eMessageType, T payload,
            IEndPointContext? context) where T : notnull
        {
            var serialized = _serialization.Serialize(payload, context);
            await _interaction.PostMessageUntrustedAsync(new NetworkMessageHeader
                { Id = id, MessageType = eMessageType, Data = serialized });
        }
    }
}
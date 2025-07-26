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
        private readonly IChannelInteractionModule _interaction;
        private readonly IContextualSerializationToolKit _serialization;

        public RepresentationModule(IChannelInteractionModule interaction,
            IContextualSerializationToolKit serialization)
        {
            _interaction = interaction;
            _serialization = serialization;
        }


        public int Id => (_interaction ?? throw new NullReferenceException("Interaction is not initialized"))
            .ConnectionId;

        public IEndPointContext Context => _interaction.Context;

        public async Task<(object? Deserialized, EMessageType MessageType)> GetSingle(
            Predicate<NetworkMessage> rule, IEndPointContext? context,
            CancellationToken token = default, params Func<NetworkMessage, Type?>[] converter)
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
            Predicate<NetworkMessage> rule, IEndPointContext? context,
            [EnumeratorCancellation] CancellationToken token = default,
            params Func<NetworkMessage, Type?>[] converter)
        {
            var writer = _interaction.ReceiveChanel.Writer;
            await foreach (var message in _interaction.ReceiveChanel.Reader.ReadAllAsync(token))
            {
                if (!rule(message))
                {
                    await writer.WriteAsync(message, token);
                    continue;
                }


                for (var i = 0; i < converter.Length; i++)
                {
                    var func = converter[i];
                    if (func(message) is { } t)
                    {
                        var deserialized = _serialization.Deserialize(message.Data, t, context);
                        yield return (deserialized, message.MessageType)!;
                        break;
                    }
                }
            }
        }

        public async Task PostCallMessageAsync<T>(Guid id, EMessageType eMessageType, T payload,
            IEndPointContext? context) where T : notnull
        {
            var serialized = _serialization.Serialize(payload, context);
            await _interaction.PostMessageAsync(new NetworkMessage
                { Id = id, MessageType = eMessageType, Data = serialized });
        }

        public void PostCallMessage<T>(Guid id, EMessageType eMessageType, T payload, IEndPointContext? context)
            where T : notnull
        {
            PostCallMessageAsync(id, eMessageType, payload, context).GetAwaiter().GetResult();
        }

        public async Task PostCallMessageUntrustedAsync<T>(Guid id, EMessageType eMessageType, T payload,
            IEndPointContext? context) where T : notnull
        {
            var serialized = _serialization.Serialize(payload, context);
            await _interaction.PostMessageUntrustedAsync(new NetworkMessage
                { Id = id, MessageType = eMessageType, Data = serialized });
        }
    }
}
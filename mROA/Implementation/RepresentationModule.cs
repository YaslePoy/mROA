using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using mROA.Abstract;

#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace mROA.Implementation
{
    public class RepresentationModule : IRepresentationModule
    {
        private IChannelInteractionModule _interaction;
        private IContextualSerializationToolKit _serialization;

        public RepresentationModule(IChannelInteractionModule interaction, IContextualSerializationToolKit serialization)
        {
            _interaction = interaction;
            _serialization = serialization;
        }


        public int Id => (_interaction ?? throw new NullReferenceException("Interaction is not initialized"))
            .ConnectionId;

        public IEndPointContext Context => _interaction.Context;

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
            Predicate<NetworkMessageHeader> rule, IEndPointContext? context,
            [EnumeratorCancellation] CancellationToken token = default,
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


                for (int i = 0; i < converter.Length; i++)
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
            if (_interaction == null)
                throw new NullReferenceException("Interaction toolkit is not initialized");
            if (_serialization == null)
                throw new NullReferenceException("Serialization toolkit is not initialized");

            var serialized = _serialization.Serialize(payload, context);
            await _interaction.PostMessageAsync(new NetworkMessageHeader
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
            await _interaction.PostMessageUntrustedAsync(new NetworkMessageHeader
                { Id = id, MessageType = eMessageType, Data = serialized });
        }
    }
}
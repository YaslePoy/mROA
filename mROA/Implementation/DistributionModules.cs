using System;
using System.Threading.Tasks;
using mROA.Abstract;
using mROA.Implementation.Backend;

namespace mROA.Implementation
{
    public class ChannelDistributionModule : IDistributionModule
    {
        private readonly IConnectionHub _hub;

        public ChannelDistributionModule(IConnectionHub hub)
        {
            _hub = hub;
        }

        public Action<NetworkMessage> GetDistributionAction(int clientId)
        {
            var writer = _hub.GetInteraction(clientId).ReceiveChanel.Writer; 
            return message =>
            {
                writer.TryWrite(message);
            };
        }
        
    }

    public class ExtractorFirstDistributionModule : IDistributionModule
    {
        private readonly IConnectionHub _hub;
        private readonly IContextualSerializationToolKit _serialization;
        private readonly HubRequestExtractor _extractorHub;

        public ExtractorFirstDistributionModule(HubRequestExtractor extractorHub, IConnectionHub hub, IContextualSerializationToolKit serialization)
        {
            _extractorHub = extractorHub;
            _hub = hub;
            _serialization = serialization;
        }

        public Action<NetworkMessage> GetDistributionAction(int clientId)
        {
            var interaction = _hub.GetInteraction(clientId);
            var writer = interaction.ReceiveChanel.Writer;
            var context = interaction.Context;
            var requestExtractor = _extractorHub[clientId];
            var converters = requestExtractor.Converters;
            var serialization = _serialization.Clone();
            return message =>
            {
                if (requestExtractor.Rule(message))
                {
                    for (var i = 0; i < converters.Length; i++)
                    {
                        var func = converters[i];
                        if (func(message) is not { } t) continue;

                        var deserialized = serialization.Deserialize(message.Data, t, context)!;
                        Task.Run(() => requestExtractor.PushMessage(deserialized, message.MessageType));
                        break;
                    }

                    return;
                }

                writer.WriteAsync(message).ConfigureAwait(false);
            };
        }
    }
}
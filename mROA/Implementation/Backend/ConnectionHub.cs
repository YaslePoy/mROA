using System;
using System.Collections.Generic;
using mROA.Abstract;

namespace mROA.Implementation.Backend
{
    public class ConnectionHub : IConnectionHub
    {
        private readonly Dictionary<int, IChannelInteractionModule> _connections = new();

        public void RegisterInteraction(IChannelInteractionModule interaction)
        {
            _connections.Add(interaction.ConnectionId, interaction);
        }

        public IChannelInteractionModule GetInteraction(int id)
        {
            return _connections!.GetValueOrDefault(id, null) ?? _connections!.GetValueOrDefault(-id, null) ??
                throw new Exception("No connection found");
        }
    }
}
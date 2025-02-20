using System;
using System.Collections.Generic;
using mROA.Abstract;

namespace mROA.Implementation.Backend
{
    public class ConnectionHub : IConnectionHub
    {
        private readonly Dictionary<int, INextGenerationInteractionModule> _connections = new();
        private ISerializationToolkit? _serializationToolkit;
    
        public void RegisterInteraction(INextGenerationInteractionModule interaction)
        {
            if (_serializationToolkit is null)
                throw new NullReferenceException("Serialization toolkit is null");
        
            _connections.Add(interaction.ConnectionId, interaction);
            var module = new RepresentationModule();
            module.Inject(_serializationToolkit);
            module.Inject(interaction);
            OnConnected?.Invoke(module);
        }

        public INextGenerationInteractionModule GetInteracion(int id)
        {
            return _connections!.GetValueOrDefault(id, null) ?? throw new Exception("No connection found");
        }

        public event ConnectionHandler? OnConnected;
        public event DisconnectionHandler? OnDisconnected;
        public void Inject<T>(T dependency)
        {
            if (dependency is ISerializationToolkit serializationToolkit) 
                _serializationToolkit = serializationToolkit;
        
        }
    }
}
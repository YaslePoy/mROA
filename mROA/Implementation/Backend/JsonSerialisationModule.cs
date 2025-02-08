﻿using System.Text;
using System.Text.Json;
using global::System.Linq;
using mROA.Abstract;

namespace mROA.Implementation.Backend
{
    public class JsonSerialisationModule : ISerialisationModule
    {
        private IInteractionModule? _dataSource;
        private IExecuteModule? _executeModule;
        private IMethodRepository? _methodRepository;

        public void HandleIncomingRequest(int clientId, byte[] command)
        {
            DefaultCallRequest request = JsonSerializer.Deserialize<DefaultCallRequest>(command)!;

            if (request.Parameter is not null)
            {
                var parameter = _methodRepository!.GetMethod(request.CommandId).GetParameters().First().ParameterType;
                request.Parameter = ((JsonElement)request.Parameter).Deserialize(parameter);
            }

            var response = _executeModule!.Execute(request);
            response.ClientId = clientId;
            PostResponse(response);
        }

        public void PostResponse(ICommandExecution call)
        {
            var texted = JsonSerializer.Serialize(call, call.GetType());
            var binary = Encoding.UTF8.GetBytes(texted);
            _dataSource!.SendTo(call.ClientId, binary);
        }

        public void Inject<T>(T dependency)
        {
            if (dependency is IInteractionModule interactionModule)
                _dataSource = interactionModule;
            if (dependency is IExecuteModule executeModule)
                _executeModule = executeModule;
            if (dependency is IMethodRepository methodRepository)
                _methodRepository = methodRepository;
        }

    }
}
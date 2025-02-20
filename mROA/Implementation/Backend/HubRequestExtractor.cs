using System;
using mROA.Abstract;

namespace mROA.Implementation.Backend
{
    public class HubRequestExtractor : IInjectableModule
    {
        private IConnectionHub? _hub;

        private IContextRepository? _contextRepository;
        private IMethodRepository? _methodRepository;
        private ISerializationToolkit? _serializationToolkit;
        private IExecuteModule? _executeModule;
        private readonly Type _extractorType;

        public HubRequestExtractor(Type extractorType)
        {
            _extractorType = extractorType;
        }

        public void Inject<T>(T dependency)
        {
            switch (dependency)
            {
                case IConnectionHub connectionHub:
                    _hub = connectionHub;
                    _hub.OnConnected += HubOnOnConnected;
                    break;
                case IContextRepository contextRepository:
                    _contextRepository = contextRepository;
                    break;
                case IMethodRepository methodRepository:
                    _methodRepository = methodRepository;
                    break;
                case ISerializationToolkit serializationToolkit:
                    _serializationToolkit = serializationToolkit;
                    break;
                case IExecuteModule executeModule:
                    _executeModule = executeModule;
                    break;
            }
        }

        private void HubOnOnConnected(IRepresentationModule interaction)
        {
            var extractor = (IRequestExtractor)Activator.CreateInstance(_extractorType)!;
            extractor.Inject(interaction);
            if (_contextRepository is IContextRepositoryHub contextHub)
                extractor.Inject(contextHub.GetRepository(interaction.Id));
            else
                extractor.Inject(interaction);
            extractor.Inject(_methodRepository);
            extractor.Inject(_serializationToolkit);
            extractor.Inject(_executeModule);
            _ = extractor.StartExtraction();
        }
    }
}
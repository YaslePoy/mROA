using mROA.Abstract;
using mROA.Implementation.Frontend;

namespace mROA.Implementation.Backend
{
    public class HubRequestExtractor : IInjectableModule
    {
        private IConnectionHub? _hub;

        private IContextRepository? _contextRepository;
        private IContextRepository? _remoteContextRepository;
        private IMethodRepository? _methodRepository;
        private IContextualSerializationToolKit? _serializationToolkit;
        private IExecuteModule? _executeModule;

        public void Inject<T>(T dependency)
        {
            switch (dependency)
            {
                case IConnectionHub connectionHub:
                    _hub = connectionHub;
                    _hub.OnConnected += HubOnOnConnected;
                    break;
                case MultiClientContextRepository:
                case ContextRepository:
                    _contextRepository = dependency as IContextRepository;
                    break;
                case RemoteContextRepository remoteContextRepository:
                    _remoteContextRepository = remoteContextRepository;
                    break;
                case IMethodRepository methodRepository:
                    _methodRepository = methodRepository;
                    break;
                case IContextualSerializationToolKit serializationToolkit:
                    _serializationToolkit = serializationToolkit;
                    break;
                case IExecuteModule executeModule:
                    _executeModule = executeModule;
                    break;
            }
        }

        private void HubOnOnConnected(IRepresentationModule interaction)
        {
            var extractor = CreateExtractor(interaction);
            extractor.StartExtraction().ContinueWith(_ => OnDisconnected(interaction));
        }

        private void OnDisconnected(IRepresentationModule representationModule)
        {
            if (_contextRepository is IContextRepositoryHub contextHub)
                contextHub.FreeRepository(representationModule.Id);
        }
        
        private IRequestExtractor CreateExtractor(IRepresentationModule interaction)
        {
            var extractor = new RequestExtractor();
            var context = new EndPointContext
            {
                HostId = 0, OwnerId = -interaction.Id
            };
            extractor.Inject(interaction);
            if (_contextRepository is IContextRepositoryHub contextHub)
                context.RealRepository = contextHub.GetRepository(interaction.Id);
            else
                context.RealRepository = _contextRepository!;
            
            context.RemoteRepository = _remoteContextRepository!;
            
            extractor.Inject(context);
            extractor.Inject(_methodRepository);
            extractor.Inject(_serializationToolkit);
            extractor.Inject(_executeModule);
            return extractor;
        }
    }
}
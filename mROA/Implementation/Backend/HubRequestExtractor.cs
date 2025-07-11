using mROA.Abstract;
using mROA.Implementation.Frontend;

namespace mROA.Implementation.Backend
{
    public class HubRequestExtractor
    {
        private IConnectionHub _hub;

        private IRealStoreInstanceRepository _contextRepository;
        private IInstanceRepository _remoteContextRepository;
        private IMethodRepository _methodRepository;
        private IContextualSerializationToolKit _serializationToolkit;
        private IExecuteModule _executeModule;

        public HubRequestExtractor(IConnectionHub hub, IRealStoreInstanceRepository contextRepository,
            RemoteInstanceRepository remoteContextRepository, IMethodRepository methodRepository,
            IContextualSerializationToolKit serializationToolkit, IExecuteModule executeModule)
        {
            _hub = hub;
            _hub.OnConnected += HubOnOnConnected;

            _contextRepository = contextRepository;
            _remoteContextRepository = remoteContextRepository;
            _methodRepository = methodRepository;
            _serializationToolkit = serializationToolkit;
            _executeModule = executeModule;
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
            var extractor = new RequestExtractor(_executeModule, _methodRepository, interaction, _serializationToolkit, interaction.Context);
            var context = interaction.Context;
            if (_contextRepository is IContextRepositoryHub contextHub)
                context.RealRepository = contextHub.GetRepository(interaction.Id);
            else
                context.RealRepository = _contextRepository;

            context.RemoteRepository = _remoteContextRepository;
            
            return extractor;
        }
    }
}
using Microsoft.Extensions.Options;
using mROA.Abstract;
using mROA.Implementation.Frontend;

namespace mROA.Implementation.Backend
{
    public class HubRequestExtractor
    {
        private readonly IRealStoreInstanceRepository _contextRepository;
        private readonly IInstanceRepository _remoteContextRepository;
        private readonly IExecuteModule _executeModule;
        private readonly DistributionOptions _mode;

        public HubRequestExtractor(IRealStoreInstanceRepository contextRepository,
            IInstanceRepository remoteContextRepository, IExecuteModule executeModule,
            IOptions<DistributionOptions> mode)
        {
            _contextRepository = contextRepository;
            _remoteContextRepository = remoteContextRepository;
            _executeModule = executeModule;
            _mode = mode.Value;
        }

        public IRequestExtractor HubOnOnConnected(IRepresentationModule interaction)
        {
            var extractor = CreateExtractor(interaction);
            if (_mode.DistributionType == EDistributionType.Channeled)
            {
                extractor.StartExtraction().ContinueWith(_ => OnDisconnected(interaction));
            }

            return extractor;
        }

        private void OnDisconnected(IRepresentationModule representationModule)
        {
            if (_contextRepository is IContextRepositoryHub contextHub)
                contextHub.FreeRepository(representationModule.Id);
        }

        private IRequestExtractor CreateExtractor(IRepresentationModule interaction)
        {
            var extractor = new RequestExtractor(_executeModule, interaction, interaction.Context);
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
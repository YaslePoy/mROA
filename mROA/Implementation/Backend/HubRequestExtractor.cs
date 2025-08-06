using System.Collections.Generic;
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
        private readonly Dictionary<int, IRequestExtractor> _producedExtractors = new();

        public HubRequestExtractor(IRealStoreInstanceRepository contextRepository,
            IInstanceRepository remoteContextRepository, IExecuteModule executeModule,
            IOptions<DistributionOptions> mode)
        {
            _contextRepository = contextRepository;
            _remoteContextRepository = remoteContextRepository;
            _executeModule = executeModule;
            _mode = mode.Value;
        }

        public IRequestExtractor this[int id] => _producedExtractors[id];

        public IRequestExtractor HubOnOnConnected(IRepresentationModule representationModule)
        {
            var extractor = CreateExtractor(representationModule);
            if (_mode.DistributionType == EDistributionType.Channeled)
            {
                extractor.StartExtraction().ContinueWith(_ => OnDisconnected(representationModule));
            }

            _producedExtractors[representationModule.Id] = extractor;
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
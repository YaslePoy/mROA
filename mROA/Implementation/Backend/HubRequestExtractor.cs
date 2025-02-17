using mROA.Abstract;
using mROA.Implementation.Frontend;

namespace mROA.Implementation.Backend;

public class HubRequestExtractor : IInjectableModule
{
    private IConnectionHub _hub;
    
    private IContextRepository? _contextRepository;
    private IMethodRepository? _methodRepository;
    private ISerializationToolkit? _serializationToolkit;
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
        }
    }

    private void HubOnOnConnected(IRepresentationModule interaction)
    {
        var extractor = new RequestExtractor();
        extractor.Inject(interaction);
        extractor.Inject(_contextRepository);
        extractor.Inject(_methodRepository);
        extractor.Inject(_serializationToolkit);
        _ = extractor.StartExtraction();
    }
}
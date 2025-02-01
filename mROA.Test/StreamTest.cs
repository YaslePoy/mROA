using mROA.Implementation;

namespace mROA.Test;

public class StreamTest
{
    private StreamBasedInteractionModule _interactionModule;
    private ISerialisationModule _serialisationModule;
    private IExecuteModule _executeModule;
    private IMethodRepository _methodRepository;
    private IContextRepository _contextRepository;
    [SetUp]
    public void Setup()
    {
        _interactionModule = new StreamBasedInteractionModule();
       
        _serialisationModule = new JsonSerialisationModule(_interactionModule, _methodRepository);

        _executeModule = new MockExecModule();
        _serialisationModule.SetExecuteModule(_executeModule);
    }

    [Test]
    public void StreamingTest()
    {
        
    }
}
using System.Diagnostics;
using System.Reflection;
using mROA.Implementation;

namespace mROA.Test;

public class Tests
{
    private ProgramlyInteractionChanel _interactionModule;
    private ISerialisationModule _serialisationModule;
    private IExecuteModule _executeModule;
    private IMethodRepository _methodRepository;
    private IContextRepository _contextRepository;
    
    private ITestController _testController;
    
    [SetUp]
    public void Setup()
    {
        _interactionModule = new ProgramlyInteractionChanel();
        _serialisationModule = new JsonSerialisationModule(_interactionModule);
        var repo = new MethodRepository();
        repo.CollectForAssembly(Assembly.GetExecutingAssembly());
        _methodRepository = repo;
        var repo2 = new ContextRepository();
        repo2.FillSingletons(Assembly.GetExecutingAssembly());
        _contextRepository = repo2;
        
        _executeModule = new PrepairedExecutionModule(_methodRepository, _serialisationModule, _contextRepository);

    }

    [Test]
    public void CommandPipelineTest()
    {
        var sw = Stopwatch.StartNew();

        _interactionModule.PassCommand(132, """
                                            {
                                              "RequestTypeId": 0,
                                              "CommandId": 2
                                            }
                                            """);
        Assert.Pass(sw.ElapsedMilliseconds.ToString());
    }

    [Test]
    public void CommandPipelineTestAsync()
    {
        var sw = Stopwatch.StartNew();
        _interactionModule.PassCommand(132, """
                                            {
                                              "RequestTypeId": 0,
                                              "CommandId": 3
                                            }
                                            """);
        while (_interactionModule.OutputBuffer.Count != 2) ;
        
        Assert.Pass(sw.ElapsedMilliseconds.ToString());
    }
    
    [Test]
    public void MethodRegistrationTest()
    {
        var repo = new MethodRepository();
        repo.CollectForAssembly(Assembly.GetExecutingAssembly());
        Assert.That(repo.GetMethods().ToList().Count == 4);
    }
    
    [Test]
    public void ContextSupplyTest()
    {
        var repo = new ContextRepository();
        repo.FillSingletons(Assembly.GetExecutingAssembly());
        var singleObject = repo.GetSingleObject(typeof(ITestController)) as ITestController;
        singleObject.B();
        Assert.That(singleObject.B() == 6);
    }
}
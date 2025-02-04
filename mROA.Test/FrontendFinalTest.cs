using System.Net;
using System.Net.Sockets;
using System.Reflection;
using mROA.Implementation;

namespace mROA.Test;

public class FrontendFinalTest
{
    private StreamBasedInteractionModule _interactionModule;
    private StreamBasedFrontendInteractionModule _frontendInteractionModule;
    private JsonFrontendSerialisationModule _frontendSerialisationModule;
    private ISerialisationModule _serialisationModule;
    private IExecuteModule _executeModule;
    private IMethodRepository _methodRepository;
    private IContextRepository _contextRepository;
    bool isTestNotFinished = true;
    private IContextRepository _frontendContextRepository;

    [SetUp]
    public void Setup()
    {
        var repo = new MethodRepository();
        repo.CollectForAssembly(Assembly.GetExecutingAssembly());
        _methodRepository = repo;
        var repo2 = new ContextRepository();
        repo2.FillSingletons(Assembly.GetExecutingAssembly());
        _contextRepository = repo2;
        _interactionModule = new StreamBasedInteractionModule();

        _serialisationModule = new JsonSerialisationModule(_interactionModule, _methodRepository);

        _executeModule = new LaunchReadyExecutionModule(_methodRepository, _serialisationModule, _contextRepository);
        TransmissionConfig.BackendRepository = _contextRepository;

        _frontendInteractionModule = new StreamBasedFrontendInteractionModule();
        _frontendSerialisationModule = new JsonFrontendSerialisationModule(_frontendInteractionModule);
        _frontendContextRepository = new FrontendContextRepository(new Dictionary<Type, Type> {
            { typeof(ITestController), typeof(TestControllerRemoteEndpoint) },
            {typeof(ITestParameter), typeof(TestParameterRemoteEndpoint)}
        }, _frontendSerialisationModule);
        TransmissionConfig.FrontendRepository = _frontendContextRepository;
        TransmissionConfig.SetupBackendRepository();
        
        Task.Run(() =>
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 4567);
            listener.Start();

            var stream = listener.AcceptTcpClient().GetStream();

            Console.WriteLine("Client connected");

            _interactionModule.RegisterClient(stream);

            while (isTestNotFinished) ;
        });
        
        var tcpClient = new TcpClient();
        tcpClient.Connect(IPAddress.Loopback, 4567);
        _frontendInteractionModule.ServerStream = tcpClient.GetStream();
        
    }

    [Test]
    public void CallTest()
    {

        
        var singleton = _frontendContextRepository.GetSingleObject(typeof(ITestController)) as ITestController;

        var x = singleton.B();
        Console.WriteLine(x);
    }

    [Test]
    public void TransmittionTest()
    {
        var singleton = _frontendContextRepository.GetSingleObject(typeof(ITestController)) as ITestController;

        var next = singleton.SharedObjectTransmitionTest().Value;
        var parameter = singleton.GetTestParameter().Value;
        var x = next.Parametrized(new TestParameter { A = 100, LinkedObject = new (parameter!)});
        
        

    }
}
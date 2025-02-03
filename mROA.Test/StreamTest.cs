using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using mROA.Implementation;
using mROA.Implementation.Test;

namespace mROA.Test;

public class StreamTest
{
    private StreamBasedInteractionModule _interactionModule;
    private StreamBasedFrontendInteractionModule _frontendInteractionModule;
    private JsonFrontendSerialisationModule _frontendSerialisationModule;
    private ISerialisationModule _serialisationModule;
    private IExecuteModule _executeModule;
    bool isTestNotFinished = true;

    [SetUp]
    public void Setup()
    {
        _interactionModule = new StreamBasedInteractionModule();

        _serialisationModule = new JsonSerialisationModule(_interactionModule, new MockMethodRepository());

        _executeModule = new MockExecModule();
        _serialisationModule.SetExecuteModule(_executeModule);

        _frontendInteractionModule = new StreamBasedFrontendInteractionModule();
        _frontendSerialisationModule = new JsonFrontendSerialisationModule(_frontendInteractionModule);
        
        Task.Run(() =>
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 4567);
            listener.Start();

            var stream = listener.AcceptTcpClient().GetStream();

            Console.WriteLine("Client connected");

            _interactionModule.RegisterClient(stream);

            while (isTestNotFinished) ;
        });
    }

    [Test]
    public void StreamingTest()
    {
        var tcpClient = new TcpClient();
        tcpClient.Connect(IPAddress.Loopback, 4567);
        _frontendInteractionModule.ServerStream = tcpClient.GetStream();
        
        var req = new JsonCallRequest { CommandId = 1, ObjectId = -1 };
        _frontendSerialisationModule.PostCallRequest(req);
        var res = ((JsonElement)_frontendSerialisationModule.GetNextCommandExecution<FinalCommandExecution>(req.CallRequestId).GetAwaiter().GetResult().Result!).Deserialize<MockResult>();
        isTestNotFinished = false;
        Assert.That(res.A == "wqer" && res.B == 5);
    }

    [Test]
    public void RemoteObjectTest()
    {
        
    }
    
}
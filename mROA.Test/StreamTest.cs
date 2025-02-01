using System.Net;
using System.Net.Sockets;
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

    [SetUp]
    public void Setup()
    {
        _interactionModule = new StreamBasedInteractionModule();

        _serialisationModule = new JsonSerialisationModule(_interactionModule, new MockMethodRepository());

        _executeModule = new MockExecModule();
        _serialisationModule.SetExecuteModule(_executeModule);

        _frontendInteractionModule = new StreamBasedFrontendInteractionModule();
        _frontendSerialisationModule = new JsonFrontendSerialisationModule(_frontendInteractionModule);
    }

    [Test]
    public void StreamingTest()
    {
        bool isTestNotFinished = true;
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
        
        var req = new JsonCallRequest { CommandId = 1, ObjectId = -1 };
        _frontendSerialisationModule.PostCallRequest(req);
        var res =_frontendSerialisationModule.GetNextCommandExecution<FinalCommandExecution>(req.CallRequestId).Result;
        isTestNotFinished = false;
    }
}
// using System.Net;
// using System.Net.Sockets;
// using System.Reflection;
// using mROA.Abstract;
// using mROA.Codegen;
// using Example.Shared;
// using mROA.Implementation;
//
// namespace mROA.Test;
//
// public class FrontendFinalTest
// {
//     private StreamBasedInteractionModule _interactionModule;
//     private StreamBasedFrontendInteractionModule _frontendInteractionModule;
//     private JsonFrontendSerialisationModule _frontendSerialisationModule;
//     private ISerialisationModule _serialisationModule;
//     private IExecuteModule _executeModule;
//     private IMethodRepository _methodRepository;
//     private IContextRepository _contextRepository;
//     bool isTestNotFinished = true;
//     private IContextRepository _frontendContextRepository;
//
//     [SetUp]
//     public void Setup()
//     {
//         _methodRepository = new CoCodegenMethodRepository();
//         var repo2 = new ContextRepository();
//         repo2.FillSingletons(typeof(ITestController).Assembly);
//         _contextRepository = repo2;
//
//         _interactionModule = new StreamBasedInteractionModule();
//
//         _serialisationModule = new JsonSerialisationModule();
//
//         _executeModule = new BasicExecutionModule();
//
//         IInjectableModule[] backendModules =
//             [_methodRepository, _contextRepository, _interactionModule, _serialisationModule, _executeModule];
//
//         foreach (var backendModule in backendModules)
//         foreach (var injection in backendModules)
//             backendModule.Inject(injection);
//
//         _frontendInteractionModule = new StreamBasedFrontendInteractionModule();
//         _frontendSerialisationModule = new JsonFrontendSerialisationModule();
//         _frontendContextRepository = new FrontendContextRepository();
//
//         IInjectableModule[] frontendModules =
//             [_frontendInteractionModule, _frontendSerialisationModule, _frontendContextRepository];
//
//         foreach (var backendModule in frontendModules)
//         foreach (var injection in frontendModules)
//             backendModule.Inject(injection);
//
//         Task.Run(() =>
//         {
//             TcpListener listener = new TcpListener(IPAddress.Loopback, 4567);
//             listener.Start();
//
//             var stream = listener.AcceptTcpClient().GetStream();
//
//             Console.WriteLine("Client connected");
//
//             _interactionModule.RegisterSourse(stream);
//
//             while (isTestNotFinished) ;
//         });
//
//         var tcpClient = new TcpClient();
//         tcpClient.Connect(IPAddress.Loopback, 4567);
//         _frontendInteractionModule.ServerStream = tcpClient.GetStream();
//     }
//
//     [Test]
//     public void CallTest()
//     {
//         var singleton = _frontendContextRepository.GetSingleObject(typeof(ITestController)) as ITestController;
//
//         var x = singleton.B();
//         Console.WriteLine(x);
//     }
//
//     [Test]
//     public void TransmittionTest()
//     {
//         var singleton = _frontendContextRepository.GetSingleObject(typeof(ITestController)) as ITestController;
//
//         var next = singleton.SharedObjectTransmitionTest().Value;
//         var parameter = singleton.GetTestParameter().Value;
//         var x = next.Parametrized(new TestParameter { A = 100, LinkedObject = new(parameter!) });
//     }
// }
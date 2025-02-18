using System.Net;
using Example.Frontend;
using Example.Shared;
using mROA.Codegen;
using mROA.Implementation;
using mROA.Implementation.Backend;
using mROA.Implementation.Bootstrap;
using mROA.Implementation.Frontend;

var builder = new FullMixBuilder();
new RemoteTypeBinder();
builder.Modules.Add(new JsonSerializationToolkit());
builder.Modules.Add(new RemoteContextRepository());
builder.Modules.Add(new NextGenerationInteractionModule());
builder.Modules.Add(new RepresentationModule());
builder.Modules.Add(new NetworkFrontendBridge(new IPEndPoint(IPAddress.Loopback, 4567)));
builder.Modules.Add(new StaticRepresentationModuleProducer());
// builder.Modules.Add(new BasicExecutionModule());
// builder.Modules.Add(new CoCodegenMethodRepository());
// builder.Modules.Add(new StreamBasedVirtualBackendInteractionModule());
// builder.Modules.Add(new JsonSerialisationModule());
builder.UseCollectableContextRepository();
builder.Build();


TransmissionConfig.RealContextRepository = builder.GetModule<ContextRepository>();
TransmissionConfig.RemoteEndpointContextRepository = builder.GetModule<RemoteContextRepository>();

builder.GetModule<NetworkFrontendBridge>()!.Connect();
// builder.GetModule<StreamBasedVirtualBackendInteractionModule>()!.StartVirtualInteraction();

Console.WriteLine(TransmissionConfig.OwnershipRepository!.GetOwnershipId());
var context = builder.GetModule<RemoteContextRepository>();

var factory = context.GetSingleObject(typeof(IPrinterFactory)) as IPrinterFactory;

//правильный порядок команд 8-5-10-7
var printer = factory.Create("Test");
var name = printer.Value.GetName();
Console.WriteLine("Printer name : {0}", name);


factory.Register(new SharedObject<IPrinter>(new ClientBasedPrinter()));
Console.WriteLine("Registered printer");

var registred = factory.GetFirstPrinter();
Console.WriteLine(registred.Value);
Console.WriteLine("Collecting all printers");
var names = factory.CollectAllNames();
Console.WriteLine(names);
Console.ReadLine();

// var page = await printer.Value.Print("Test Page", new CancellationToken());
// var data = page.Value.GetData();
// Console.WriteLine("Data : {0}", Encoding.UTF8.GetString(data));
//
// var loadSingleton = context.GetSingleObject(typeof(ILoadTest)) as ILoadTest;
//
// const int iterations = 10000;
// var timer = Stopwatch.StartNew();
// var x = 0;
// for (int i = 0; i < iterations; i++)
// {
//     x = loadSingleton.Next(x);
// }
//
// timer.Stop();
// Console.WriteLine("X is {0}", x);
// Console.WriteLine("Time : {0}", timer.Elapsed.TotalMilliseconds);
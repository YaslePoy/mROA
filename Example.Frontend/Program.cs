// See https://aka.ms/new-console-template for more information


using System.Diagnostics;
using System.Net;
using System.Text;
using Example.Shared;
using mROA.Codegen;
using mROA.Implementation;
using mROA.Implementation.Bootstrap;
using mROA.Implementation.Frontend;


var mixer = new FullMixBuilder();
new RemoteTypeBinder();
mixer.Modules.Add(new RemoteContextRepository());
mixer.Modules.Add(new JsonFrontendSerialisationModule());
mixer.Modules.Add(new StreamBasedFrontendInteractionModule());
mixer.Modules.Add(new NetworkFrontendBridge(new IPEndPoint(IPAddress.Loopback, 4567)));
mixer.Build();


TransmissionConfig.RealContextRepository = mixer.GetModule<RemoteContextRepository>();
TransmissionConfig.RemoteEndpointContextRepository = mixer.GetModule<RemoteContextRepository>();
mixer.GetModule<NetworkFrontendBridge>().Connect();

Console.WriteLine(TransmissionConfig.ProcessOwnerId);
var context = mixer.GetModule<RemoteContextRepository>();

var factory = context.GetSingleObject(typeof(IPrinterFactory)) as IPrinterFactory;



var printer = factory.Create("Test");
var name = printer.Value.GetName();
Console.WriteLine("Printer name : {0}", name);
var page = await printer.Value.Print("Test Page", new CancellationToken());
var data = page.Value.GetData();
Console.WriteLine("Data : {0}", Encoding.UTF8.GetString(data));

var loadSingleton = context.GetSingleObject(typeof(ILoadTest)) as ILoadTest;

const int iterations = 10000;
var timer = Stopwatch.StartNew();
var x = 0;
for (int i = 0; i < iterations; i++)
{
    x = loadSingleton.Next(x);
}
timer.Stop();
Console.WriteLine("X is {0}", x);
Console.WriteLine("Time : {0}", timer.Elapsed.TotalMilliseconds);
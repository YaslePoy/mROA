// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Text;
using Example.Shared;
using mROA.Codegen;
using mROA.Implementation;
using mROA.Implementation.Bootstrap;

var mixer = new FullMixBuilder();

mixer.Modules.Add(new FrontendContextRepository());
mixer.Modules.Add(new JsonFrontendSerialisationModule());
mixer.Modules.Add(new StreamBasedInteractionModule());
mixer.Modules.Add(new NetworkFrontendBridge(new IPEndPoint(IPAddress.Loopback, 4567)));

mixer.Build();
var context = mixer.GetModule<FrontendContextRepository>();

var factory = context.GetSingleObject(typeof(IPrinterFactory)) as IPrinterFactory;

var printer = factory.Create("Test");
var name = printer.Value.GetName();
Console.WriteLine("Printer name : {0}", name);
var page = printer.Value.Print("Test Page");
var data = page.Value.GetData();
Console.WriteLine("Data : {0}", Encoding.UTF8.GetString(data));
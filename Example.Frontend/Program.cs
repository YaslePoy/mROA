// See https://aka.ms/new-console-template for more information


using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using Example.Shared;
using mROA.Codegen;
using mROA.Implementation.Bootstrap;
using mROA.Implementation.Frontend;

public class Program
{
    public static void Main(string[] args)
    {
        var mixer = new FullMixBuilder();

        mixer.Modules.Add(new FrontendContextRepository());
        mixer.Modules.Add(new JsonFrontendSerialisationModule());
        mixer.Modules.Add(new StreamBasedFrontendInteractionModule());
        mixer.Modules.Add(new NetworkFrontendBridge(new IPEndPoint(IPAddress.Loopback, 4567)));

        mixer.Build();

        mixer.GetModule<NetworkFrontendBridge>().Connect();
        var context = mixer.GetModule<FrontendContextRepository>();

        var factory = context.GetSingleObject(typeof(IPrinterFactory)) as IPrinterFactory;


        var printer = factory.Create("Test");
        var name = printer.Value.GetName();
        Console.WriteLine("Printer name : {0}", name);
        var page = printer.Value.Print("Test Page", new CancellationToken()).GetAwaiter().GetResult();
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
    }
}

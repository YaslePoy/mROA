using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Example.Frontend;
using Example.Shared;
using mROA.Codegen;
using mROA.Implementation;
using mROA.Implementation.Backend;
using mROA.Implementation.Bootstrap;
using mROA.Implementation.Frontend;

class Program
{
    public static void Main(string[] args)
    {
        var builder = new FullMixBuilder();
        new RemoteTypeBinder();
        builder.Modules.Add(new JsonSerializationToolkit());
        builder.Modules.Add(new RemoteContextRepository());
        builder.Modules.Add(new NextGenerationInteractionModule());
        builder.Modules.Add(new RepresentationModule());
        builder.Modules.Add(new NetworkFrontendBridge(new IPEndPoint(IPAddress.Loopback, 4567)));
        builder.Modules.Add(new StaticRepresentationModuleProducer());
        builder.Modules.Add(new RequestExtractor());
        builder.Modules.Add(new BasicExecutionModule());
        builder.Modules.Add(new CoCodegenMethodRepository());
        builder.UseCollectableContextRepository();
        builder.Modules.Add(new CancellationRepository());

        builder.Build();


        TransmissionConfig.RealContextRepository = builder.GetModule<ContextRepository>();
        TransmissionConfig.RemoteEndpointContextRepository = builder.GetModule<RemoteContextRepository>();

        builder.GetModule<NetworkFrontendBridge>()!.Connect();
        _ = builder.GetModule<RequestExtractor>()!.StartExtraction();
        Console.WriteLine(TransmissionConfig.OwnershipRepository.GetOwnershipId());
        var context = builder.GetModule<RemoteContextRepository>();

        var factory = context.GetSingleObject(typeof(IPrinterFactory)) as IPrinterFactory;

//правильный порядок команд 8-5-10-7
        var printer = factory.Create("Test");
        using (var disposingPrinter = printer.Value)
        {
            Console.WriteLine("Printer created");
            Thread.Sleep(100);

            var name = disposingPrinter.GetName();
            Console.WriteLine("Printer name : {0}", name);

            Thread.Sleep(100);

            factory.Register(new SharedObject<IPrinter>(new ClientBasedPrinter()));
            Console.WriteLine("Registered printer");
            Thread.Sleep(100);


            var registred = factory.GetFirstPrinter();
            Console.WriteLine("First printer");
            Thread.Sleep(100);

            Console.WriteLine(registred.Value);
            Console.WriteLine("Collecting all printers");
            var names = factory.CollectAllNames();
            Thread.Sleep(100);

            Console.WriteLine(string.Join(", ", names));

            var page = disposingPrinter.Print("Test Page", new CancellationToken()).GetAwaiter().GetResult();
            Console.WriteLine("Page printed");
            Console.WriteLine(page.Value.ToString());
            var data = page.Value.GetData();
            Console.WriteLine("Data : {0}", Encoding.UTF8.GetString(data));

            Console.WriteLine("Dispose printer");
        }


        var loadSingleton = context.GetSingleObject(typeof(ILoadTest)) as ILoadTest;


        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var t = Task.Run(async () => await loadSingleton!.AsyncTest(token));

        Thread.Sleep(5000);
        cts.Cancel();
        Console.WriteLine($"Token state {cts.Token.IsCancellationRequested}");
        Console.ReadKey();
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
        // Console.WriteLine($"Time per call: {timer.Elapsed.TotalMilliseconds / iterations} ms");
    }
}
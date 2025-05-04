using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Example.Frontend;
using Example.Shared;
using mROA.Abstract;
using mROA.Cbor;
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
        // builder.Modules.Add(new JsonSerializationToolkit());
        builder.Modules.Add(new CborSerializationToolkit());
        builder.Modules.Add(new EndPointContext());
        builder.Modules.Add(new RemoteContextRepository());
        builder.Modules.Add(new ChannelInteractionModule());
        builder.Modules.Add(new UdpUntrustedInteraction());
        builder.Modules.Add(new RepresentationModule());
        var serverEndPoint = new IPEndPoint(IPAddress.Loopback, 4567);
        builder.Modules.Add(new NetworkFrontendBridge(serverEndPoint));
        builder.Modules.Add(new StaticRepresentationModuleProducer());
        builder.Modules.Add(new RequestExtractor());
        builder.Modules.Add(new BasicExecutionModule());
        builder.Modules.Add(new CoCodegenMethodRepository());
        builder.UseCollectableContextRepository();
        builder.Modules.Add(new CancellationRepository());

        builder.Build();


        TransmissionConfig.RealContextRepository = builder.GetModule<ContextRepository>();
        TransmissionConfig.RemoteEndpointContextRepository = builder.GetModule<RemoteContextRepository>();

        var frontendBridge = builder.GetModule<IFrontendBridge>()!;
        frontendBridge.Connect();
        _ = builder.GetModule<RequestExtractor>()!.StartExtraction();
        _ = builder.GetModule<UdpUntrustedInteraction>().Start(serverEndPoint);
        Console.WriteLine(TransmissionConfig.OwnershipRepository.GetOwnershipId());
        var context = builder.GetModule<RemoteContextRepository>();

        var factory =
            context.GetSingleObject(typeof(IPrinterFactory),
                -TransmissionConfig.OwnershipRepository.GetHostOwnershipId()) as IPrinterFactory;

        using (var disposingPrinter = factory.Create("Test"))
        {
            DemoCheck.CreatingPrinter = true;
            disposingPrinter.OnPrint += (_, _) =>
            {
                Console.WriteLine("New page creater. Called from event!!!");
                DemoCheck.EventCallback = true;
            };
            Console.WriteLine("Printer created");
            Thread.Sleep(100);

            frontendBridge.Obstacle();
            var name = disposingPrinter.GetName();
            DemoCheck.BasicNonParamsCall = true;
            Console.WriteLine("Printer name : {0}", name);

            Thread.Sleep(100);

            disposingPrinter.SomeoneIsApproaching("Mikhail");
            Console.WriteLine("Approaching detected");
            
            factory.Register(new ClientBasedPrinter());
            DemoCheck.ClientBasedImplementation = true;
            Console.WriteLine("Registered printer");
            Thread.Sleep(100);


            var registered = factory.GetFirstPrinter();
            Console.WriteLine("First printer");
            Thread.Sleep(100);

            Console.WriteLine(registered);
            Console.WriteLine("Collecting all printers");
            var names = factory.CollectAllNames();
            Thread.Sleep(100);

            Console.WriteLine(string.Join(", ", names));

            var page = disposingPrinter.Print("Test Page", false, default, CancellationToken.None).GetAwaiter()
                .GetResult();
            Console.WriteLine("Page printed");
            DemoCheck.TaskExecution = true;
            Console.WriteLine(page.ToString());

            Console.WriteLine($"Printer resource : {disposingPrinter.Resource}");
            DemoCheck.PropertyGet = true;

            Console.WriteLine("Restoring resource");
            disposingPrinter.Resource = 100;
            DemoCheck.PropertySet = true;
            Console.WriteLine($"Printer resource again : {disposingPrinter.Resource}");

            var data = page.GetData();
            Console.WriteLine("Data : {0}", Encoding.UTF8.GetString(data));

            Console.WriteLine("Dispose printer");
        }

        DemoCheck.Dispose = true;


        var loadSingleton = context.GetSingleObject(typeof(ILoadTest), 0) as ILoadTest;


        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var t = Task.Run(async () => await loadSingleton!.AsyncTest(token));

        Thread.Sleep(5000);
        cts.Cancel();
        Console.WriteLine($"Token state {cts.Token.IsCancellationRequested}");
        DemoCheck.TaskCancelation = true;

        frontendBridge.Disconnect();

        DemoCheck.Show();
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
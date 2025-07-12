using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Example.Frontend;
using Example.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using mROA.Abstract;
using mROA.Cbor;
using mROA.Codegen;
using mROA.Implementation;
using mROA.Implementation.Backend;
using mROA.Implementation.Frontend;


class Program
{
    public static async Task Main(string[] args)
    {
        new RemoteTypeBinder();

        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Services.AddSingleton<IContextualSerializationToolKit, CborSerializationToolkit>();
        builder.Services.AddSingleton<IEndPointContext, EndPointContext>();
        builder.Services.AddSingleton<IRealStoreInstanceRepository, InstanceRepository>(provider =>
        {
            var repo = new InstanceRepository(provider.GetService<IRepresentationModuleProducer>());
            repo.FillSingletons(typeof(Program).Assembly);
            return repo;
        });
        
        
        builder.Services.AddSingleton<IInstanceRepository, RemoteInstanceRepository>();
        builder.Services.AddSingleton<IChannelInteractionModule, ChannelInteractionModule>();
        builder.Services.AddSingleton<IUntrustedInteractionModule, UdpUntrustedInteraction>();
        builder.Services.AddSingleton<IRepresentationModule, RepresentationModule>();
        var serverEndPoint = new IPEndPoint(IPAddress.Loopback, 4567);
        builder.Services.AddSingleton<IFrontendBridge, NetworkFrontendBridge>();
        builder.Services.AddOptions();
        builder.Services.Configure<GatewayOptions>(options => options.Endpoint = serverEndPoint);
        builder.Services.AddSingleton<IRepresentationModuleProducer, StaticRepresentationModuleProducer>();
        builder.Services.AddSingleton<IRequestExtractor, RequestExtractor>();
        builder.Services.AddSingleton<IExecuteModule, BasicExecutionModule>();
        
        builder.Services.AddSingleton<IMethodRepository, CollectableMethodRepository>(p =>
        {
            var methodRepo = new CollectableMethodRepository();
            methodRepo.AppendInvokers(new GeneratedInvokersCollection());
            return methodRepo;
        });
        builder.Services.AddSingleton<ICallIndexProvider, GeneratedCallIndexProvider>();
        builder.Services.AddSingleton<ICancellationRepository, CancellationRepository>();

        var app = builder.Build();

        var frontendBridge = app.Services.GetService<IFrontendBridge>()!;
        await frontendBridge.Connect();
        _ = app.Services.GetService<IRequestExtractor>()!.StartExtraction();
        _ = app.Services.GetService<IUntrustedInteractionModule>().Start(serverEndPoint);
        Console.WriteLine(app.Services.GetService<IEndPointContext>().HostId);
        var context = app.Services.GetService<IInstanceRepository>();

#if !JUST_LOAD
        var factory =
            context.GetSingletonObject<IPrinterFactory>(
                app.Services.GetService<IEndPointContext>());

        using (var disposingPrinter = factory.Create("Test"))
        {
            disposingPrinter.SetFingerPrint(new[] { 1, 2, 3 }).ContinueWith(r => { Console.WriteLine(r.Status); });

            await disposingPrinter.IntTest(new MyData { Id = 5, Score = 7, Name = "Test" });
            DemoCheck.CreatingPrinter = true;
            disposingPrinter.OnPrint += (_, _) =>
            {
                Console.WriteLine("New page created. Called from event!!!");
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
            factory.Register(disposingPrinter);
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

            Console.WriteLine("Names: " + string.Join(", ", names));

            var page = await disposingPrinter.Print("Test Page", false, default, CancellationToken.None);
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
#endif

        var loadSingleton = context.GetSingletonObject<ILoadTest>(app.Services.GetService<IEndPointContext>());

#if !JUST_LOAD
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var t = Task.Run(async () => await loadSingleton!.AsyncTest(token));

        Thread.Sleep(5000);
        cts.Cancel();
        Console.WriteLine($"Token state {cts.Token.IsCancellationRequested}");
        DemoCheck.TaskCancelation = true;
#endif

        const int iterations = 10_000;
        var timer = Stopwatch.StartNew();
        var x = 0;
        for (int i = 0; i < iterations; i++)
        {
            x = await loadSingleton.Next(x);
        }

        timer.Stop();
        Console.WriteLine("X is {0}", x);
        Console.WriteLine("Time : {0}", timer.Elapsed.TotalMilliseconds);
        Console.WriteLine($"Time per call: {timer.Elapsed.TotalMilliseconds / iterations} ms");
        Console.WriteLine($"Serialization time: {CborSerializationToolkit.SerializationTime}");

        frontendBridge.Disconnect();

        DemoCheck.Show();
    }
}
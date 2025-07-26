using System.Net;
using Example.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using mROA.Abstract;
using mROA.Cbor;
using mROA.Codegen;
using mROA.Implementation;
using mROA.Implementation.Backend;
using mROA.Implementation.Frontend;


const int C = 100;
var time = TimeSpan.FromSeconds(10);
Console.WriteLine($"Starting bench for {time} from {C} connections");
var cts = new CancellationTokenSource();
new RemoteTypeBinder();
var eps = await GetLoadEndpoints(C);
var tasks = new Task<int>[C];
for (int i = 0; i < C; i++)
{
    tasks[i] = Requests(cts.Token, i, eps[i]);
}

cts.CancelAfter(time);
Console.WriteLine($"Test end at {DateTime.Now.Add(time)}");
Console.WriteLine("Start waiting");
await Task.WhenAll(tasks);
Console.WriteLine("End waiting");

var totalRequests = tasks.Sum(i => i.Result);
Console.WriteLine($"Total requests: {totalRequests:N0}");
Console.WriteLine($"Results: {totalRequests / time.TotalSeconds:N} RPS");
File.AppendAllText("results.txt", $"[SINGLE CBOR WRITER ALLOC] {totalRequests}\r\n");

async Task<List<ILoadTest>> GetLoadEndpoints(int count)
{
    try
    {
        var loads = new List<ILoadTest>();
        for (int i = 0; i < count; i++)
        {
            Console.WriteLine($"Initializing {i}");
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
            // builder.Services.AddSingleton<IUntrustedInteractionModule, UdpUntrustedInteraction>();
            builder.Services.AddSingleton<IRepresentationModule, RepresentationModule>();
            var serverEndPoint = new IPEndPoint(IPAddress.Loopback, 4567);
            builder.Services.AddSingleton<IFrontendBridge, NetworkFrontendBridge>();
            builder.Services.AddOptions();
            builder.Services.Configure<GatewayOptions>(options => options.Endpoint = serverEndPoint);
            builder.Services.AddSingleton<IRepresentationModuleProducer, StaticRepresentationModuleProducer>();
            // builder.Services.AddSingleton<IRequestExtractor, RequestExtractor>();
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

            Console.WriteLine($"Connecting {i}");
            var frontendBridge = app.Services.GetService<IFrontendBridge>()!;
            await frontendBridge.Connect();
            // _ = app.Services.GetService<IRequestExtractor>()!.StartExtraction();
            // _ = app.Services.GetService<IUntrustedInteractionModule>().Start(serverEndPoint);
            var context = app.Services.GetService<IInstanceRepository>();
            Console.WriteLine($"Connected {i}");

            var singletonObject =
                context.GetSingletonObject<ILoadTest>(
                    app.Services.GetService<IEndPointContext>());
            loads.Add(singletonObject);
        }

        return loads;
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}

async Task<int> Requests(CancellationToken token, int id, ILoadTest load)
{
    try
    {
        int count = 0;
        while (true)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            await load.Next(2);
            count++;
        }

        Console.WriteLine(id);
        return count;
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}
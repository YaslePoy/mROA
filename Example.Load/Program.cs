using System.Diagnostics;
using System.Net;
using System.Runtime;
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
var tasks = new Task<(int, double[])>[C];
for (int i = 0; i < C; i++)
{
    tasks[i] = Requests(cts.Token, i, eps[i]);
}

cts.CancelAfter(time);
Console.WriteLine($"Test end at {DateTime.Now.Add(time)}");
Console.WriteLine("Start waiting");
await Task.WhenAll(tasks);
Console.WriteLine("End waiting");

var totalRequests = tasks.Sum(i => i.Result.Item1);
var totalLatency = tasks.SelectMany(i => i.Result.Item2).ToList();
totalLatency.Sort();
var n = totalLatency.Count;

var p50 = totalLatency[(int)(n * 50f / 100f)];
var p95 = totalLatency[(int)(n * 95f / 100f)];
var p99 = totalLatency[(int)(n * 99f / 100f)];
Console.WriteLine($"Total requests: {totalRequests:N0}");
Console.WriteLine($"Results: {totalRequests / time.TotalSeconds:N} RPS");
Console.WriteLine("Latency (µs): p50={0} p95={1} p99={2}", p50, p95, p99);
File.AppendAllText("results.txt", $"[FAST ID] {totalRequests}\r\n");

async Task<List<ILoadTest>> GetLoadEndpoints(int count)
{
    try
    {
        var loads = new List<ILoadTest>();
        for (int i = 0; i < count; i++)
        {
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

            var frontendBridge = app.Services.GetService<IFrontendBridge>()!;
            await frontendBridge.Connect();
            // _ = app.Services.GetService<IRequestExtractor>()!.StartExtraction();
            // _ = app.Services.GetService<IUntrustedInteractionModule>().Start(serverEndPoint);
            var context = app.Services.GetService<IInstanceRepository>();

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

async Task<(int, double[])> Requests(CancellationToken token, int id, ILoadTest load)
{
    try
    {
        var latencyList = new List<double>();
        var sw = new Stopwatch();
        int count = 0;
        while (true)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            sw.Restart();
            await load.Next(2);
            sw.Stop();
            latencyList.Add(sw.Elapsed.TotalMicroseconds);
            count++;
        }
        
        return (count,  latencyList.ToArray());
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}
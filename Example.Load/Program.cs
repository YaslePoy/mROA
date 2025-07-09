using System.Net;
using Example.Shared;
using mROA.Abstract;
using mROA.Cbor;
using mROA.Codegen;
using mROA.Implementation;
using mROA.Implementation.Backend;
using mROA.Implementation.Bootstrap;
using mROA.Implementation.Frontend;

const int C = 100;
var time = TimeSpan.FromSeconds(10);
Console.WriteLine($"Starting bench for {time} from {C} connections");
var cts = new CancellationTokenSource();
new RemoteTypeBinder();

var tasks = new Task<int>[C];
for (int i = 0; i < C; i++)
{
    tasks[i] = Requests(cts.Token);
}

cts.CancelAfter(time);
Console.WriteLine("Start waiting");
await Task.WhenAll(tasks);
Console.WriteLine("End waiting");

var totalRequests = tasks.Sum(i => i.Result);
Console.WriteLine($"Total requests: {totalRequests}");
Console.WriteLine($"Results: {totalRequests / time.TotalSeconds:N} RPS");

async Task<int> Requests(CancellationToken token)
{
    var builder = new FullMixBuilder();

    builder.Modules.Add(new CborSerializationToolkit());
    builder.Modules.Add(new EndPointContext());
    builder.Modules.Add(new RemoteInstanceRepository());
    builder.Modules.Add(new ChannelInteractionModule());
    // builder.Modules.Add(new UdpUntrustedInteraction());
    builder.Modules.Add(new RepresentationModule());
    var serverEndPoint = new IPEndPoint(IPAddress.Loopback, 4567);
    builder.Modules.Add(new NetworkFrontendBridge(serverEndPoint));
    builder.Modules.Add(new StaticRepresentationModuleProducer());
    // builder.Modules.Add(new RequestExtractor());
    builder.Modules.Add(new BasicExecutionModule());
    var methodRepo = new CollectableMethodRepository();
    methodRepo.AppendInvokers(new GeneratedInvokersCollection());
    builder.Modules.Add(methodRepo);
    builder.Modules.Add(new GeneratedCallIndexProvider());
    builder.UseCollectableContextRepository();
    builder.Modules.Add(new CancellationRepository());

    builder.Build();

    var frontendBridge = builder.GetModule<IFrontendBridge>()!;
    await frontendBridge.Connect();
    // _ = builder.GetModule<RequestExtractor>()!.StartExtraction();
    // _ = builder.GetModule<UdpUntrustedInteraction>().Start(serverEndPoint);
    var context = builder.GetModule<RemoteInstanceRepository>();

    var factory =
        context.GetSingletonObject<ILoadTest>(
            builder.GetModule<IEndPointContext>());

    int count = 0;
    int a;
    do
    {
        a = await factory.Next(2);
        count++;
    } while (!token.IsCancellationRequested);

    Console.WriteLine(a);
    return count;
}
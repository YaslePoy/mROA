using System;
using System.Net;
using Example.Backend;
using Example.Shared;
using mROA.Abstract;
using mROA.Cbor;
using mROA.Codegen;
using mROA.Implementation;
using mROA.Implementation.Backend;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddLogging(l => l.AddConsole());
        builder.Services.AddSingleton<IContextualSerializationToolKit, CborSerializationToolkit>();
        builder.Services.AddSingleton<IIdentityGenerator, BackendIdentityGenerator>();
        builder.Services.AddSingleton<IGatewayModule, NetworkGatewayModule>();
        builder.Services.AddSingleton<IUntrustedGateway, UdpGateway>();
        builder.Services.AddSingleton<IConnectionHub, ConnectionHub>();
        
        builder.Services.AddOptions();
        var listening = new IPEndPoint(IPAddress.Any, 4567);
        builder.Services.Configure<GatewayOptions>(options => options.Endpoint = listening);
        builder.Services.Configure<DistributionOptions>(o => o.DistributionType = EDistributionType.ExtractorFirst);
        builder.Services.AddSingleton<IDistributionModule, ExtractorFirstDistributionModule>();
        
        builder.Services.AddSingleton<HubRequestExtractor>();
        builder.Services.AddSingleton<IExecuteModule, BasicExecutionModule>();
        builder.Services.AddSingleton<IRepresentationModuleProducer, CreativeRepresentationModuleProducer>();
        builder.Services.AddSingleton<IInstanceRepository, RemoteInstanceRepository>();
        builder.Services.AddSingleton<IRealStoreInstanceRepository>(provider => new MultiClientInstanceRepository(i =>
        {
            var producer = provider.GetService<IRepresentationModuleProducer>();
            var repo = new InstanceRepository(producer);
            repo.FillSingletons(typeof(PrinterFactory).Assembly);

            return repo;
        }));

        builder.Services.AddSingleton<IMethodRepository>(p =>
        {
            var methodRepo = new CollectableMethodRepository();
            methodRepo.AppendInvokers(new GeneratedInvokersCollection());
            return methodRepo;
        });
        builder.Services.AddSingleton<ICallIndexProvider, GeneratedCallIndexProvider>();

        builder.Services.AddSingleton<ICancellationRepository, CancellationRepository>();

        var host = builder.Build();
//
        new RemoteTypeBinder();
//
        _ = host.Services.GetService<IUntrustedGateway>()!.Start();
        var gateway = host.Services.GetService<IGatewayModule>();
        gateway.Run();

        Console.ReadLine();
    }
}
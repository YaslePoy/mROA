using System;
using System.Linq;
using System.Net;
using Example.Backend;
using Example.Shared;
using mROA.Abstract;
using mROA.Cbor;
using mROA.Codegen;
using mROA.Implementation;
using mROA.Implementation.Backend;
using mROA.Implementation.Bootstrap;

class Program
{
    public static void Main(string[] args)
    {
        var builder = new FullMixBuilder();
        // builder.UseJsonSerialisation();
        builder.Modules.Add(new CborSerializationToolkit());
        builder.Modules.Add(new BackendIdentityGenerator());
        // builder.UseNetworkGateway(new IPEndPoint(IPAddress.Loopback, 4567), typeof(NextGenerationInteractionModule),
        //     builder.GetModule<IIdentityGenerator>()!);
        var listening = new IPEndPoint(IPAddress.Any, 4567);
        builder.UseNetworkGateway(listening, typeof(ChannelInteractionModule),
            builder.GetModule<IIdentityGenerator>()!);
        builder.Modules.Add(new UdpGateway(listening));
        builder.Modules.Add(new ConnectionHub());
        builder.Modules.Add(new HubRequestExtractor());

        builder.UseBasicExecution();
        builder.Modules.Add(new CreativeRepresentationModuleProducer(
            new IInjectableModule[] { builder.GetModule<IContextualSerializationToolKit>()! },
            typeof(RepresentationModule)));
        builder.Modules.Add(new RemoteInstanceRepository());
// builder.UseCollectableContextRepository(typeof(PrinterFactory).Assembly);
        builder.Modules.Add(new MultiClientInstanceRepository(i =>
        {
            var repo = new InstanceRepository();
            repo.FillSingletons(typeof(PrinterFactory).Assembly);
            repo.Inject(builder.Modules.OfType<CreativeRepresentationModuleProducer>().First());
            return repo;
        }));
        var methodRepo = new CollectableMethodRepository();
        methodRepo.AppendInvokers(new GeneratedInvokersCollection());
        builder.Modules.Add(methodRepo);
        builder.Modules.Add(new GeneratedCallIndexProvider());
        builder.Modules.Add(new GeneratedCallIndexProvider());
        builder.Modules.Add(new CancellationRepository());

        builder.Build();
        new RemoteTypeBinder();


        _ = builder.GetModule<UdpGateway>()!.Start();
        var gateway = builder.GetModule<IGatewayModule>();
        gateway.Run();
        
        Console.ReadLine();
    }
}
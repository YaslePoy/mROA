using System.Linq;
using System.Net;
using Example.Backend;
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
        // builder.UseJsonSerialisation();
        builder.Modules.Add(new CborSerializationToolkit());
        builder.Modules.Add(new BackendIdentityGenerator());
        // builder.UseNetworkGateway(new IPEndPoint(IPAddress.Loopback, 4567), typeof(NextGenerationInteractionModule),
        //     builder.GetModule<IIdentityGenerator>()!);
        var listening = new IPEndPoint(IPAddress.Loopback, 4567);
        builder.UseNetworkGateway(listening, typeof(ChannelInteractionModule),
            builder.GetModule<IIdentityGenerator>()!);
        builder.Modules.Add(new UdpGateway(listening));
        builder.Modules.Add(new ConnectionHub());
        builder.Modules.Add(new HubRequestExtractor(typeof(RequestExtractor)));

        builder.UseBasicExecution();
        builder.Modules.Add(new CreativeRepresentationModuleProducer(
            new IInjectableModule[] { builder.GetModule<ISerializationToolkit>()! },
            typeof(RepresentationModule)));
        builder.Modules.Add(new RemoteContextRepository());
// builder.UseCollectableContextRepository(typeof(PrinterFactory).Assembly);
        builder.Modules.Add(new MultiClientContextRepository(i =>
        {
            var repo = new ContextRepository();
            repo.FillSingletons(typeof(PrinterFactory).Assembly);
            repo.Inject(builder.Modules.OfType<CreativeRepresentationModuleProducer>().First());
            return repo;
        }));
        builder.SetupMethodsRepository(new CoCodegenMethodRepository());

        builder.Modules.Add(new CancellationRepository());

        builder.Build();
        new RemoteTypeBinder();

        TransmissionConfig.RealContextRepository = builder.GetModule<MultiClientContextRepository>()!;
        TransmissionConfig.RemoteEndpointContextRepository = builder.GetModule<RemoteContextRepository>()!;
        TransmissionConfig.OwnershipRepository = new MultiClientOwnershipRepository();

        _ = builder.GetModule<UdpGateway>()!.Start();
        var gateway = builder.GetModule<IGatewayModule>();
        gateway.Run();
    }
}
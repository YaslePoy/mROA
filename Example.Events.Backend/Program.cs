﻿using System.Net;
using mROA.Abstract;
using mROA.Cbor;
using mROA.Codegen;
using mROA.Implementation;
using mROA.Implementation.Backend;
using mROA.Implementation.Bootstrap;
using mROA.Implementation.Frontend;

namespace Example.Events.Backend;

class Program
{
    static void Main(string[] args)
    {
        var builder = new FullMixBuilder();
        builder.Modules.Add(new CborSerializationToolkit());
        builder.Modules.Add(new BackendIdentityGenerator());
        builder.UseNetworkGateway(IPEndPoint.Parse("192.168.1.101:6000"), typeof(NextGenerationInteractionModule),
            builder.GetModule<IIdentityGenerator>()!);

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
            repo.FillSingletons(typeof(ChatFactory).Assembly);
            repo.Inject(builder.Modules.OfType<CreativeRepresentationModuleProducer>().First());
            return repo;
        }));
        builder.SetupMethodsRepository(new CoCodegenMethodRepository());

        builder.Modules.Add(new CancellationRepository());

        builder.Build();
        new RemoteTypeBinder();

        TransmissionConfig.RealContextRepository = builder.GetModule<MultiClientContextRepository>();
        TransmissionConfig.RemoteEndpointContextRepository = builder.GetModule<RemoteContextRepository>();
        TransmissionConfig.OwnershipRepository = new MultiClientOwnershipRepository();

        var gateway = builder.GetModule<IGatewayModule>();

        gateway.Run();
    }
}
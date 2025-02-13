using System.Net;
using Example.Backend;
using mROA.Abstract;
using mROA.Codegen;
using mROA.Implementation;
using mROA.Implementation.Backend;
using mROA.Implementation.Bootstrap;


var builder = new FullMixBuilder();
builder.UseJsonSerialisation();
builder.UseNetworkGateway(new IPEndPoint(IPAddress.Loopback, 4567));
builder.UseStreamInteraction();
builder.UseBasicExecution();
builder.UseCollectableContextRepository(typeof(PrinterFactory).Assembly);
builder.SetupMethodsRepository(new CoCodegenMethodRepository());
builder.Modules.Add(new StaticSerialisationModuleProducer());

builder.Build();


var gateway = builder.GetModule<IGatewayModule>() ;

gateway.Run();
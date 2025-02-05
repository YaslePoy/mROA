using System.Net;
using Example.Shared;
using mROA.Codegen;
using mROA.Implementation;
using mROA.Implementation.Bootstrap;


var bootstrap = new FullMixBuilder();
bootstrap.UseJsonSerialisation();
bootstrap.UseNetworkGateway(new IPEndPoint(IPAddress.Loopback, 4567));
bootstrap.UseStreamInteraction();
bootstrap.UseBasicExecution();
bootstrap.UseCollectableContextRepository(typeof(IPrinterFactory).Assembly);
bootstrap.SetupMethodsRepository(new CoCodegenMethodRepository());
bootstrap.Build();

var gateway = bootstrap.GetModule<IGatewayModule>();

gateway.Run();
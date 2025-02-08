﻿using System.Net;
using Example.Backend;
using mROA.Abstract;
using mROA.Codegen;
using mROA.Implementation.Backend;
using mROA.Implementation.Bootstrap;

public class Program
{
    public static void Main(string[] args)
    {
        var bootstrap = new FullMixBuilder();
        bootstrap.UseJsonSerialisation();
        bootstrap.UseNetworkGateway(new IPEndPoint(IPAddress.Loopback, 4567));
        bootstrap.UseStreamInteraction();
        bootstrap.UseBasicExecution();
        bootstrap.UseCollectableContextRepository(typeof(PrinterFactory).Assembly);
        bootstrap.SetupMethodsRepository(new CoCodegenMethodRepository());
        bootstrap.Build();


        var gateway = bootstrap.GetModule<IGatewayModule>();

        gateway.Run();
    }
}
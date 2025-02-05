using System.Net;
using System.Reflection;
using mROA.Implementation.Bootstrap;

namespace mROA.Implementation;

public static class BasicConfigurationExtensions
{
    public static void UseJsonSerialisation(this ServerBootstrap bootstrap)
    {
        bootstrap.Modules.Add(new JsonSerialisationModule());
    }

    public static void UseNetworkGateway(this ServerBootstrap bootstrap, IPEndPoint endPoint)
    {
        bootstrap.Modules.Add(new NetworkGatewayModule(endPoint));
    }

    public static void UseStreamInteraction(this ServerBootstrap bootstrap)
    {
        bootstrap.Modules.Add(new StreamBasedInteractionModule());
    }
    
    public static void UseBasicExecution(this ServerBootstrap bootstrap)
    {
        bootstrap.Modules.Add(new BasicExecutionModule());
    }

    public static void UseCollectableContextRepository(this ServerBootstrap bootstrap, params Assembly[] assemblies)
    {
        var repo = new ContextRepository();
        repo.FillSingletons(assemblies);
        bootstrap.Modules.Add(repo);
    }

    public static void SetupMethodsRepository(this ServerBootstrap bootstrap, IMethodRepository methodRepository)
    {
        bootstrap.Modules.Add(methodRepository);
    }
}
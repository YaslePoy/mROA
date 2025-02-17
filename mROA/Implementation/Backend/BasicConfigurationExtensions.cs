using System.Net;
using System.Reflection;
using mROA.Abstract;
using mROA.Implementation.Bootstrap;

namespace mROA.Implementation.Backend;

public static class BasicConfigurationExtensions
{
    public static void UseJsonSerialisation(this FullMixBuilder builder)
    {
        builder.Modules.Add(new JsonSerializationToolkit());
    }

    public static void UseNetworkGateway(this FullMixBuilder builder, IPEndPoint endPoint, Type interactionModuleType, params IInjectableModule[] injectableModules)
    {
        builder.Modules.Add(new NetworkGatewayModule(endPoint, interactionModuleType, injectableModules));
    }
    
    public static void UseBasicExecution(this FullMixBuilder builder)
    {
        builder.Modules.Add(new BasicExecutionModule());
    }

    public static void UseCollectableContextRepository(this FullMixBuilder builder, params Assembly[] assemblies)
    {
        var repo = new ContextRepository();
        repo.FillSingletons(assemblies);
        TransmissionConfig.RealContextRepository = repo;
        builder.Modules.Add(repo);
    }

    public static void SetupMethodsRepository(this FullMixBuilder builder, IMethodRepository methodRepository)
    {
        builder.Modules.Add(methodRepository);
    }
}
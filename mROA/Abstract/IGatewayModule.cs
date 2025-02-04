namespace mROA.Implementation;

public interface IGatewayModule : IDisposable
{
    void Configure(IInteractionModule interactionModule);
    void Run();
}
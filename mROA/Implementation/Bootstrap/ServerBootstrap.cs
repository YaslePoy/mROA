using mROA.Abstract;

namespace mROA.Implementation.Bootstrap;

public interface IBootstrap
{
    void Build();
}

public class ServerBootstrap : IBootstrap
{
    public List<IInjectableModule> Modules { get; private set; } = new();
    public void Build()
    {
        foreach (var module in Modules)
        foreach (var injection in Modules)
            module.Inject(injection);
    }

    public T GetModule<T>()
    {
        return Modules.OfType<T>().FirstOrDefault();
    }
}
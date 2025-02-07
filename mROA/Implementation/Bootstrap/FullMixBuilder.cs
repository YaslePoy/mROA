using mROA.Abstract;

namespace mROA.Implementation.Bootstrap;

public interface IRoaBuilder
{
    void Build();
}

public class FullMixBuilder : IRoaBuilder
{
    public List<IInjectableModule> Modules { get; } = new();
    public void Build()
    {
        foreach (var module in Modules)
        foreach (var injection in Modules)
            module.Inject(injection);
    }

    public T? GetModule<T>()
    {
        return Modules.OfType<T>().FirstOrDefault();
    }
}
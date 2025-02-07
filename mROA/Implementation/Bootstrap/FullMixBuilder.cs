using mROA.Abstract;

namespace mROA.Implementation.Bootstrap;

public class FullMixBuilder
{
    public List<IInjectableModule> Modules { get; } = [];

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
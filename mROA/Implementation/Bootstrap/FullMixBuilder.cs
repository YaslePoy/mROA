using System.Collections.Generic;
using System.Linq;
using mROA.Abstract;

namespace mROA.Implementation.Bootstrap
{
    public class FullMixBuilder
    {
        public List<IInjectableModule> Modules { get; } = new();

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

        public bool TryGetModule<T>(out IInjectableModule module) where T : IInjectableModule
        {
            var mod = Modules.OfType<T>().FirstOrDefault();
            if (mod is null)
            {
                module = default;
                return false;
            }

            module = mod;
            return true;
        }
    }
}
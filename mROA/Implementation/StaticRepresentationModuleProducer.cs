using System;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class StaticRepresentationModuleProducer : IRepresentationModuleProducer
    {
        private readonly IRepresentationModule _representationModule;

        public StaticRepresentationModuleProducer(IRepresentationModule representationModule)
        {
            _representationModule = representationModule;
        }

        public IRepresentationModule Produce(int ownership)
        {
            return _representationModule;
        }
    }
}
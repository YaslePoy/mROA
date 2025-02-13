using mROA.Abstract;

namespace mROA.Implementation;

interface ISerialisationModuleProducer : IInjectableModule
{
    ISerialisationModule.IFrontendSerialisationModule Produce(int ownership);
}
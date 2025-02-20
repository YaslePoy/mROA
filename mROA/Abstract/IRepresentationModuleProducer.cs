namespace mROA.Abstract
{
    public interface IRepresentationModuleProducer : IInjectableModule
    {
        IRepresentationModule Produce(int id);
    }
}
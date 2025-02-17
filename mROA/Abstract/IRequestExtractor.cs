namespace mROA.Abstract;

public interface IRequestExtractor : IInjectableModule
{
    Task StartExtraction();
}
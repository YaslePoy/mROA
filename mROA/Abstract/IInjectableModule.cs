namespace mROA.Abstract;

public interface IInjectableModule
{
    void Inject<T>(T dependency);
}
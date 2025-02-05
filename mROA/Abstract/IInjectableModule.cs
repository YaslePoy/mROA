namespace mROA.Abstract;

public interface IInjectableModule
{
    public void Inject<T>(T dependency);
}
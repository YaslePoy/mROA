namespace mROA.Abstract;

public interface IIdentityGenerator : IInjectableModule
{
    int GetNextIdentity();
}
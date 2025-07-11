namespace mROA.Abstract
{
    public interface IMethodRepository
    {
        IMethodInvoker GetMethod(int id);
    }
}
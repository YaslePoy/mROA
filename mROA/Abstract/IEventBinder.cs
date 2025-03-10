namespace mROA.Abstract
{
    public interface IEventBinder<T>
    {
        public void BindEvents(T source, IEndPointContext context);
    }
}
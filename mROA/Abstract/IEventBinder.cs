namespace mROA.Abstract
{
    public interface IEventBinder<in T> : IEventBinder
    {
        public void BindEvents(T source, IEndPointContext context,
            IRepresentationModuleProducer representationModuleProducer, int index);

        void IEventBinder.BindEvents(object source, IEndPointContext context,
            IRepresentationModuleProducer representationModuleProducer,
            int index)
        {
            BindEvents((T)source, context, representationModuleProducer, index);
        }
    }

    public interface IEventBinder
    {
        public void BindEvents(object source, IEndPointContext context,
            IRepresentationModuleProducer representationModuleProducer, int index);
    }
}
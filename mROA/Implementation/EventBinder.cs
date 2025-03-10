using System;

namespace mROA.Abstract
{
    public class EventBinder<T> : IEventBinder<T>
    {
        public Action<T, IEndPointContext, IRepresentationModuleProducer, int> BindAction { get; set; }

        public void BindEvents(T source, IEndPointContext context,
            IRepresentationModuleProducer representationModuleProducer, int index)
        {
            BindAction(source, context, representationModuleProducer, index);
        }
    }
}
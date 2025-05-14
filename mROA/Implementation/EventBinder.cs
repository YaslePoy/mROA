using System;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class EventBinder<T> : IEventBinder<T>
    {
        public Action<T, IEndPointContext, IRepresentationModuleProducer, int> BindAction { get; set; } = (_, _, _, _) => { };

        public void BindEvents(T source, IEndPointContext context,
            IRepresentationModuleProducer representationModuleProducer, int index)
        {
            BindAction(source, context, representationModuleProducer, index);
        }
    }
}